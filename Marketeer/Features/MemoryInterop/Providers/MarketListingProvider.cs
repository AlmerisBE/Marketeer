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

        var uiRows = this.ParseUiRowNumbers();

        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0) {
                continue;
            }

            uint quantity = (uint)item->Quantity;
            uint price = 0;

            if (i < uiRows.Count) {
                var numbersInRow = uiRows[i];
                if (numbersInRow.Count > 0) {
                    price = numbersInRow.LastOrDefault(n => n != quantity);
                    if (price == 0) {
                        price = numbersInRow.Last();
                    }
                }
            }

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId.Value,
                SlotIndex = (uint)i,
                ItemId = item->ItemId,
                Quantity = quantity,
                PricePerUnit = price
            });
        }

        return listings;
    }

    private List<List<uint>> ParseUiRowNumbers() {
        var rows = new List<List<uint>>();
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList", 1);

        if (addonPtr == nint.Zero) {
            return rows;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null) {
            return rows;
        }

        try {
            AtkComponentNode* listComponent = null;

            for (int i = 0; i < addon->UldManager.NodeListCount; i++) {
                var node = addon->UldManager.NodeList[i];
                if (node != null && node->Type == NodeType.Component) {
                    var compNode = (AtkComponentNode*)node;
                    if (compNode->Component->UldManager.NodeListCount == 20) {
                        listComponent = compNode;
                        break;
                    }
                }
            }

            if (listComponent != null) {
                for (int j = 0; j < listComponent->Component->UldManager.NodeListCount; j++) {
                    var itemNode = listComponent->Component->UldManager.NodeList[j];
                    var numbers = new List<uint>();

                    // Trigger deep recursive scan for this specific UI row
                    this.ExtractNumbersRecursively(itemNode, numbers);

                    rows.Add(numbers);
                }

                rows.Reverse();
            }
        }
        catch {
            // Silently abort on memory access violations
        }

        return rows;
    }

    // Bulletproof recursive scanner: hunts down any numeric text node nested inside the component tree
    private void ExtractNumbersRecursively(AtkResNode* node, List<uint> numbers) {
        if (node == null) {
            return;
        }

        if (node->Type == NodeType.Text) {
            var textNode = (AtkTextNode*)node;
            string text = textNode->NodeText.ToString();

            string numericString = new string(text.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(numericString) && uint.TryParse(numericString, out uint val)) {
                numbers.Add(val);
            }
        }
        else if (node->Type == NodeType.Component) {
            var compNode = (AtkComponentNode*)node;
            for (int i = 0; i < compNode->Component->UldManager.NodeListCount; i++) {
                this.ExtractNumbersRecursively(compNode->Component->UldManager.NodeList[i], numbers);
            }
        }
    }
}