using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.Features.Inventory.Models;
using System.Collections.Generic;

namespace Marketeer.Features.Inventory.Contracts;

public interface IInventoryService {
    int GetItemCountInInventory(uint itemId);
    uint GetRetainerMarketItemPrice(int slotIndex);
    IReadOnlyList<InventorySlotInfo> GetInventorySlots(InventoryType inventoryType);
    int GetUiIndexForRetainerMarketItem(int slotIndex);
}