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
        var atkValues = this.GetAllAtkValues(addonPtr);

        var config = this.configService.GetConfig();
        var knownListings = new List<TrackedListing>();

        lock (config) {
            if (config.KnownListings != null) {
                knownListings = config.KnownListings.ToList();
            }
        }

        // Mathematical Block Discovery: Instantly find Unit Price and Total Price arrays
        int? unitPriceBlockStartIndex = null;
        int? totalPriceBlockStartIndex = null;

        if (atkValues.Count >= 20) {
            for (int a = 0; a <= atkValues.Count - 20; a++) {
                for (int b = 0; b <= atkValues.Count - 20; b++) {
                    if (a == b) {
                        continue;
                    }

                    bool isValidPair = true;
                    int matchWeight = 0;

                    for (int k = 0; k < 20; k++) {
                        var item = container->GetInventorySlot(k);
                        // Skip empty inventory slots during calibration
                        if (item == null || item->ItemId == 0u) {
                            continue;
                        }

                        uint qty = (uint)item->Quantity;
                        ulong? valA = atkValues[a + k];
                        ulong? valB = atkValues[b + k];

                        if (valA == null || valB == null) {
                            isValidPair = false;
                            break;
                        }

                        // The absolute mathematical rule of FFXIV Market Board
                        if (valA.Value * qty != valB.Value) {
                            isValidPair = false;
                            break;
                        }

                        if (valA.Value > 0u) {
                            matchWeight++;
                        }
                    }

                    if (isValidPair && matchWeight > 0) {
                        unitPriceBlockStartIndex = a;
                        totalPriceBlockStartIndex = b;
                        break;
                    }
                }

                if (unitPriceBlockStartIndex != null) {
                    break;
                }
            }
        }

        if (unitPriceBlockStartIndex != null) {
            this.logger.Debug($"[MarketListing] Memory block calibrated successfully. Instant extraction enabled.");
        }

        // Instant Extraction Loop (No scrolling required)
        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0u) {
                continue;
            }

            uint quantity = (uint)item->Quantity;
            string name = this.GetItemName(item->ItemId);
            uint pricePerUnit = 0u;
            uint totalPrice = 0u;

            if (unitPriceBlockStartIndex.HasValue && totalPriceBlockStartIndex.HasValue) {
                pricePerUnit = (uint)atkValues[unitPriceBlockStartIndex.Value + i].GetValueOrDefault();
                totalPrice = (uint)atkValues[totalPriceBlockStartIndex.Value + i].GetValueOrDefault();
            }

            // Fallback for edge cases where memory blocks are shifting
            if (pricePerUnit == 0u) {
                var hist = knownListings.FirstOrDefault(l => l.AssociatedRetainerId == activeRetainerId.Value && l.SlotIndex == i);
                if (hist != null && hist.PricePerUnit > 0u) {
                    pricePerUnit = hist.PricePerUnit;
                    totalPrice = pricePerUnit * quantity;
                }
            }

            // Standard FFXIV retainer tax is mathematically around 5% of total price
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

        // FFXIV offsets High-Quality item IDs by 1,000,000 in memory
        uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        if (itemSheet != null && itemSheet.HasRow(baseItemId)) {
            return itemSheet.GetRow(baseItemId).Name.ToString();
        }

        return string.Empty;
    }

    // Extracts all numeric values from the unmanaged network payload array
    private List<ulong?> GetAllAtkValues(nint addonPtr) {
        var values = new List<ulong?>();
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
                        string numericString = new string(rawText.Where(char.IsDigit).ToArray());

                        if (ulong.TryParse(numericString, out ulong parsedVal)) {
                            values.Add(parsedVal);
                        }
                        else {
                            values.Add(null);
                        }
                    }
                    catch {
                        values.Add(null);
                    }
                }
                else {
                    values.Add(null);
                }
            }
            else if (val.Type == AtkValueType.UInt || val.Type == AtkValueType.Int) {
                ulong num = val.Type == AtkValueType.UInt ? val.UInt : (ulong)val.Int;
                values.Add(num);
            }
            else {
                values.Add(null);
            }
        }

        return values;
    }
}