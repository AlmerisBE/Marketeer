using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Models;
using System.Collections.Generic;

namespace Marketeer.API.GameInterop.Contracts;

public interface IInventoryService {
    int GetItemCountInInventory(uint itemId);
    uint GetRetainerMarketItemPrice(int slotIndex);
    IReadOnlyList<InventorySlotInfo> GetInventorySlots(InventoryType inventoryType);
    int GetUiIndexForRetainerMarketItem(int slotIndex);
}