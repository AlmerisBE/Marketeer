using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Models;
using System.Linq;

namespace Marketeer.API.InventoryTracking.Services;

public class InventoryDiffService : IInventoryDiffService {
    public InventoryDiff Compare(InventorySnapshot oldSnapshot, InventorySnapshot newSnapshot) {
        var diff = new InventoryDiff();

        var oldItems = oldSnapshot.Items.ToList();
        var newItems = newSnapshot.Items.ToList();

        // 1. Check exact slot matches (Same position, same item)
        for (int i = oldItems.Count - 1; i >= 0; i--) {
            var oldItem = oldItems[i];
            var matchIndex = newItems.FindIndex(n => n.EqualsLocation(oldItem));

            if (matchIndex >= 0) {
                var newItem = newItems[matchIndex];

                if (oldItem.ItemId == newItem.ItemId) {
                    if (oldItem.Quantity != newItem.Quantity) {
                        diff.QuantityChanged.Add(new ItemQuantityChange {
                            ItemId = oldItem.ItemId,
                            ContainerId = oldItem.ContainerId,
                            SlotIndex = oldItem.SlotIndex,
                            Difference = (int)newItem.Quantity - (int)oldItem.Quantity
                        });
                    }

                    oldItems.RemoveAt(i);
                    newItems.RemoveAt(matchIndex);
                }
            }
        }

        // 2. Check for moves (Same item, different location)
        for (int i = oldItems.Count - 1; i >= 0; i--) {
            var oldItem = oldItems[i];
            var matchIndex = newItems.FindIndex(n => n.ItemId == oldItem.ItemId && n.Quantity == oldItem.Quantity);

            if (matchIndex >= 0) {
                var newItem = newItems[matchIndex];
                diff.Moved.Add(new ItemMove {
                    ItemId = oldItem.ItemId,
                    OldContainerId = oldItem.ContainerId,
                    OldSlotIndex = oldItem.SlotIndex,
                    NewContainerId = newItem.ContainerId,
                    NewSlotIndex = newItem.SlotIndex
                });

                oldItems.RemoveAt(i);
                newItems.RemoveAt(matchIndex);
            }
        }

        // 3. Remaining items are either purely removed or purely added
        diff.Removed.AddRange(oldItems);
        diff.Added.AddRange(newItems);

        return diff;
    }
}