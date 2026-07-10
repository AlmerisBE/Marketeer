using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.MemoryInterop.Providers;

public unsafe class MarketListingProvider : IMarketListingProvider {

    public ulong? GetActiveRetainerId() {
        var manager = RetainerManager.Instance();
        if (manager == null) {
            return null;
        }

        // FFXIVClientStructs modernized this access into a method call
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

        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            // Modernized property name: ItemId
            if (item == null || item->ItemId == 0) {
                continue;
            }

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId.Value,
                ItemId = item->ItemId,
                // Explicit cast required to satisfy strict uint type constraint
                Quantity = (uint)item->Quantity,
                PricePerUnit = 1000 // Placeholder: Requires AddonRetainerSell or AgentRetainer parsing for exact price
            });
        }

        return listings;
    }
}