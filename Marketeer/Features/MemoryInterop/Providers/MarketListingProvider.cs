using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.MemoryInterop.Providers;

public unsafe class MarketListingProvider : IMarketListingProvider {
    private IGameGui gameGui;

    public MarketListingProvider(IGameGui gameGui) {
        this.gameGui = gameGui;
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

        var prices = this.ParsePricesFromAddon();

        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0) {
                continue;
            }

            uint price = 0;
            if (i < prices.Count) {
                price = prices[i];
            }

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId.Value,
                ItemId = item->ItemId,
                Quantity = (uint)item->Quantity,
                PricePerUnit = price
            });
        }

        return listings;
    }

    private List<uint> ParsePricesFromAddon() {
        var prices = new List<uint>();

        // Extract the raw address from the Dalamud wrapper struct
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList", 1);
        var addon = (AtkUnitBase*)addonPtr.Address;

        if (addon == null || !addon->IsVisible) {
            return prices;
        }

        try {
            for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
                var node = addon->UldManager.NodeList[i];
                if (node == null || node->Type != NodeType.Component) {
                    continue;
                }

                var componentNode = (AtkComponentNode*)node;
                if (componentNode->Component->UldManager.NodeListCount < 20) {
                    continue;
                }

                for (int j = 0; j < componentNode->Component->UldManager.NodeListCount; j++) {
                    var itemNode = componentNode->Component->UldManager.NodeList[j];
                    if (itemNode == null || itemNode->Type != NodeType.Component) {
                        continue;
                    }

                    var itemComponentNode = (AtkComponentNode*)itemNode;

                    for (int k = 0; k < itemComponentNode->Component->UldManager.NodeListCount; k++) {
                        var innerNode = itemComponentNode->Component->UldManager.NodeList[k];
                        if (innerNode == null || innerNode->Type != NodeType.Text) {
                            continue;
                        }

                        var textNode = (AtkTextNode*)innerNode;
                        string text = textNode->NodeText.ToString();

                        string numericString = new string(text.Where(char.IsDigit).ToArray());
                        if (!string.IsNullOrEmpty(numericString) && uint.TryParse(numericString, out uint price) && price > 0) {
                            prices.Add(price);
                            break;
                        }
                    }
                }
            }
        }
        catch {
            // Silently ignore memory read access violations to prevent plugin crashes
        }

        prices.Reverse();
        return prices;
    }
}