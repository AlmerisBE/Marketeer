using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.MemoryInterop.Providers;

public unsafe class MarketListingProvider : IMarketListingProvider {
    private IGameGui gameGui;
    private ILoggerService logger;
    private IDataManager dataManager;
    private IConfigurationService configService;

    private class UiRowData {
        public List<string> Texts = new();
        public uint UnitPrice;
        public string ItemName = string.Empty;
        public bool IsMatched;
    }

    public MarketListingProvider(
        IGameGui gameGui,
        ILoggerService logger,
        IDataManager dataManager,
        IConfigurationService configService) {
        this.gameGui = gameGui;
        this.logger = logger;
        this.dataManager = dataManager;
        this.configService = configService;
    }

    public ulong? GetActiveRetainerId() {
        var manager = RetainerManager.Instance();
        if (manager == null) {
            return null;
        }

        var activeRetainer = manager->GetActiveRetainer();
        if (activeRetainer != null && activeRetainer->RetainerId != 0u) {
            return activeRetainer->RetainerId;
        }

        return null;
    }

    public IReadOnlyList<TrackedListing> GetActiveRetainerListings() {
        var listings = new List<TrackedListing>();
        var activeRetainerId = this.GetActiveRetainerId();

        if (!activeRetainerId.HasValue) {
            return listings;
        }

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) {
            return listings;
        }

        var container = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
        if (container == null) {
            return listings;
        }

        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList", 1);
        var uiRows = this.ParseUiRows(addonPtr);
        var atkValues = this.GetAllAtkValues(addonPtr);

        var config = this.configService.GetConfig();
        var knownListings = new List<TrackedListing>();

        lock (config) {
            if (config.KnownListings != null) {
                knownListings = config.KnownListings.ToList();
            }
        }

        int priceOffset = 0;
        bool isCalibrated = false;

        // Step 1: Calibrate using visible UI rows and mathematical deduction
        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0u) {
                continue;
            }

            uint qty = (uint)item->Quantity;
            string name = this.GetItemName(item->ItemId);
            string cleanName = this.NormalizeForMatch(name);

            var matchedRow = uiRows.FirstOrDefault(r => !r.IsMatched && r.Texts.Any(t => {
                string ct = this.NormalizeForMatch(t);
                if (string.IsNullOrEmpty(ct)) {
                    return false;
                }

                return cleanName == ct || (ct.Length > 5 && cleanName.StartsWith(ct));
            }));

            if (matchedRow != null) {
                matchedRow.IsMatched = true;
                matchedRow.ItemName = name;

                var numbers = matchedRow.Texts.Select(t => {
                    var numStr = new string(t.Where(char.IsDigit).ToArray());
                    return uint.TryParse(numStr, out uint val) ? val : 0u;
                }).Where(n => n > 0u).ToList();

                if (qty == 1u) {
                    var poss = numbers.Where(n => n != 1u).ToList();
                    matchedRow.UnitPrice = poss.Count > 0 ? poss.Max() : 1u;
                }
                else {
                    foreach (var num in numbers) {
                        if (num == 0u || num == qty) {
                            continue;
                        }

                        if (numbers.Contains(num * qty)) {
                            matchedRow.UnitPrice = num;
                            break;
                        }
                    }

                    if (matchedRow.UnitPrice == 0u && numbers.Count > 0) {
                        var fallbackPrices = numbers.Where(n => n != qty).ToList();
                        if (fallbackPrices.Count > 0) {
                            matchedRow.UnitPrice = fallbackPrices.Min();
                        }
                    }
                }
            }
        }

        // Step 2: Establish the array stride offset from FFXIV's unmanaged memory structure
        var calibrationRow = uiRows.FirstOrDefault(r => r.IsMatched && r.UnitPrice > 0u);
        if (calibrationRow != null) {
            int nameIdx = atkValues.FindIndex(v => v is string s && this.NormalizeForMatch(s) == this.NormalizeForMatch(calibrationRow.ItemName));

            if (nameIdx != -1) {
                for (int j = 0; j < atkValues.Count; j++) {
                    if (atkValues[j] is uint u && u == calibrationRow.UnitPrice) {
                        int diff = Math.Abs(j - nameIdx);

                        if (diff > 0 && diff % 20 == 0) {
                            priceOffset = j - nameIdx;
                            isCalibrated = true;
                            this.logger.Debug($"[MarketListing] Memory offset calibrated perfectly at {priceOffset}.");
                            break;
                        }
                    }
                }
            }
        }

        // Step 3: Extract all data flawlessly and instantaneously without scrolling
        int lastSearchIdx = 0;
        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0u) {
                continue;
            }

            uint quantity = (uint)item->Quantity;
            string name = this.GetItemName(item->ItemId);
            uint pricePerUnit = 0u;

            if (isCalibrated) {
                int nIdx = atkValues.FindIndex(lastSearchIdx, v => v is string s && this.NormalizeForMatch(s) == this.NormalizeForMatch(name));

                if (nIdx != -1) {
                    lastSearchIdx = nIdx + 1;
                    int targetIdx = nIdx + priceOffset;

                    if (targetIdx >= 0 && targetIdx < atkValues.Count && atkValues[targetIdx] is uint extractedPrice) {
                        pricePerUnit = extractedPrice;
                    }
                }
            }

            if (pricePerUnit == 0u) {
                var hist = knownListings.FirstOrDefault(l => l.AssociatedRetainerId == activeRetainerId.Value && l.SlotIndex == i);
                if (hist != null && hist.PricePerUnit > 0u) {
                    pricePerUnit = hist.PricePerUnit;
                }
            }

            uint totalPrice = pricePerUnit * quantity;
            uint tax = (uint)Math.Floor(totalPrice * 0.05);

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId.Value,
                SlotIndex = (uint)i,
                ItemId = item->ItemId,
                ItemName = name,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                TotalPrice = totalPrice,
                Tax = tax
            });
        }

        return listings;
    }

    private string GetItemName(uint itemId) {
        var itemSheet = this.dataManager.GetExcelSheet<Item>();

        uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        if (itemSheet != null && itemSheet.HasRow(baseItemId)) {
            return itemSheet.GetRow(baseItemId).Name.ToString();
        }

        return string.Empty;
    }

    private string NormalizeForMatch(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        return new string(input.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private List<object> GetAllAtkValues(nint addonPtr) {
        var values = new List<object>();
        if (addonPtr == nint.Zero) {
            return values;
        }

        var addon = (AtkUnitBase*)addonPtr;
        for (int i = 0; i < addon->AtkValuesCount; i++) {
            var val = addon->AtkValues[i];
            var typeInt = (int)val.Type;

            if (val.Type == AtkValueType.String || typeInt == 36 || typeInt == 38) {
                var stringPointer = (byte*)val.String;

                if (stringPointer != null) {
                    try {
                        var seString = MemoryHelper.ReadSeStringNullTerminated((nint)stringPointer);
                        string rawText = seString.TextValue.Trim();

                        if (!string.IsNullOrEmpty(rawText)) {
                            values.Add(rawText);
                        }
                        else {
                            values.Add(string.Empty);
                        }
                    }
                    catch {
                        values.Add(string.Empty);
                    }
                }
                else {
                    values.Add(string.Empty);
                }
            }
            else if (val.Type == AtkValueType.UInt || val.Type == AtkValueType.Int) {
                uint num = val.Type == AtkValueType.UInt ? val.UInt : (uint)val.Int;
                values.Add(num);
            }
            else {
                values.Add(string.Empty);
            }
        }

        return values;
    }

    private List<UiRowData> ParseUiRows(nint addonPtr) {
        var rows = new List<UiRowData>();
        if (addonPtr == nint.Zero) {
            return rows;
        }

        var addon = (AtkUnitBase*)addonPtr;
        if (addon == null || addon->UldManager.NodeList == null) {
            return rows;
        }

        for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
            this.ExtractUiRowsRecursively(addon->UldManager.NodeList[i], rows, null);
        }

        return rows;
    }

    private void ExtractUiRowsRecursively(AtkResNode* node, List<UiRowData> rows, UiRowData? currentRow) {
        if (node == null) {
            return;
        }

        if (currentRow != null && node->Type == NodeType.Text && node->NodeId < 100) {
            var textNode = (AtkTextNode*)node;
            var stringPointer = (byte*)textNode->NodeText.StringPtr;

            if (stringPointer != null) {
                try {
                    var seString = MemoryHelper.ReadSeStringNullTerminated((nint)stringPointer);
                    string rawText = seString.TextValue.Trim();

                    if (!string.IsNullOrEmpty(rawText)) {
                        currentRow.Texts.Add(rawText);
                    }
                }
                catch { }
            }
        }
        else if (node->Type == NodeType.Component || (int)node->Type >= 1000) {
            var compNode = (AtkComponentNode*)node;
            bool isListItemStart = currentRow == null && (node->NodeId >= 50000 && node->NodeId < 60000);

            UiRowData? nextRow = currentRow;
            if (isListItemStart) {
                nextRow = new UiRowData { IsMatched = false };
                rows.Add(nextRow);
            }

            if (compNode->Component != null) {
                for (int i = 0; i < compNode->Component->UldManager.NodeListCount; i++) {
                    this.ExtractUiRowsRecursively(compNode->Component->UldManager.NodeList[i], rows, nextRow);
                }
            }
        }
    }
}