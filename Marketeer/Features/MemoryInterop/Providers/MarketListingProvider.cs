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
        public float Y;
        public uint TotalPrice;
        public List<string> Texts = new();
        public bool IsMatched;
    }

    public MarketListingProvider(IGameGui gameGui, ILoggerService logger, IDataManager dataManager, IConfigurationService configService) {
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
        if (activeRetainer != null && activeRetainer->RetainerId != 0) {
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

        var uiRows = this.ParseUiRows();
        var itemSheet = this.dataManager.GetExcelSheet<Item>();

        var knownListings = this.configService.GetConfig().KnownListings ?? new List<TrackedListing>();

        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0) {
                continue;
            }

            uint quantity = (uint)item->Quantity;
            uint pricePerUnit = 0;

            // FFXIV décale les ID des objets HQ de 1 000 000 en mémoire
            uint baseItemId = item->ItemId > 1000000 ? item->ItemId - 1000000 : item->ItemId;

            string itemName = string.Empty;
            if (itemSheet != null && itemSheet.HasRow(baseItemId)) {
                itemName = itemSheet.GetRow(baseItemId).Name.ToString();
            }

            string cleanItemName = this.NormalizeForMatch(itemName);

            var matchedRow = uiRows.FirstOrDefault(r =>
                !r.IsMatched &&
                !string.IsNullOrEmpty(cleanItemName) &&
                r.Texts.Any(t => {
                    string cleanUiText = this.NormalizeForMatch(t);
                    if (string.IsNullOrEmpty(cleanUiText)) {
                        return false;
                    }

                    // Permet d'associer les objets même si FFXIV tronque l'affichage avec "..."
                    if (cleanUiText.Length > 5 && cleanItemName.StartsWith(cleanUiText)) {
                        return true;
                    }

                    return cleanItemName == cleanUiText;
                })
            );

            if (matchedRow != null) {
                matchedRow.IsMatched = true;

                // On divise le prix total lu dans l'UI par la quantité réelle pour obtenir le prix unitaire
                if (quantity > 0) {
                    pricePerUnit = matchedRow.TotalPrice / quantity;
                }

                this.logger.Debug($"[MarketListing] Mapped Slot {i} ({itemName}) successfully. Total: {matchedRow.TotalPrice}, Unit: {pricePerUnit}");
            }
            else {
                // FALLBACK : L'objet a été déchargé de l'UI pendant le scroll. On restaure le dernier prix connu.
                var historicalListing = knownListings.FirstOrDefault(l =>
                    l.AssociatedRetainerId == activeRetainerId.Value &&
                    l.SlotIndex == i &&
                    l.ItemId == item->ItemId);

                if (historicalListing != null && historicalListing.PricePerUnit > 0) {
                    pricePerUnit = historicalListing.PricePerUnit;
                    this.logger.Debug($"[MarketListing] Slot {i} ({itemName}) out of UI bounds. Rescued historical price: {pricePerUnit}.");
                }
            }

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId.Value,
                SlotIndex = (uint)i,
                ItemId = item->ItemId,
                Quantity = quantity,
                PricePerUnit = pricePerUnit
            });
        }

        return listings;
    }

    private string NormalizeForMatch(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }
        return new string(input.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private List<UiRowData> ParseUiRows() {
        var rows = new List<UiRowData>();
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList", 1);

        if (addonPtr == nint.Zero) {
            return rows;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null || addon->UldManager.NodeList == null) {
            return rows;
        }

        for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
            this.ExtractUiRowsRecursively(addon->UldManager.NodeList[i], rows, 0f, null);
        }

        return rows.OrderBy(r => r.Y).ToList();
    }

    private void ExtractUiRowsRecursively(AtkResNode* node, List<UiRowData> rows, float currentY, UiRowData? currentRow) {
        if (node == null) {
            return;
        }

        float absoluteY = currentY + node->Y;

        if (currentRow != null && node->Type == NodeType.Text && node->NodeId < 100) {
            if (((uint)node->NodeFlags & 0x10) != 0) {
                var textNode = (AtkTextNode*)node;

                if (textNode->NodeText.StringPtr.Value != null) {
                    try {
                        var seString = MemoryHelper.ReadSeStringNullTerminated((nint)textNode->NodeText.StringPtr.Value);
                        string rawText = seString.TextValue.Trim();

                        if (!string.IsNullOrEmpty(rawText)) {
                            currentRow.Texts.Add(rawText);

                            if (node->NodeId == 7) {
                                string numericString = new string(rawText.Where(char.IsDigit).ToArray());
                                if (uint.TryParse(numericString, out uint val)) {
                                    currentRow.TotalPrice = val;
                                }
                            }
                        }
                    }
                    catch {
                    }
                }
            }
        }
        else if (node->Type == NodeType.Component || (int)node->Type >= 1000) {
            var compNode = (AtkComponentNode*)node;

            bool isListItemStart = currentRow == null && (node->NodeId >= 50000 && node->NodeId < 60000);

            UiRowData? nextRow = currentRow;
            if (isListItemStart) {
                nextRow = new UiRowData { Y = absoluteY, IsMatched = false };
                rows.Add(nextRow);
            }

            if (compNode->Component != null) {
                for (int i = 0; i < compNode->Component->UldManager.NodeListCount; i++) {
                    this.ExtractUiRowsRecursively(compNode->Component->UldManager.NodeList[i], rows, absoluteY, nextRow);
                }
            }
        }
    }
}