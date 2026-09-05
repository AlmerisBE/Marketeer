using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using System.Collections.Generic;

namespace Marketeer.API.GameInterop.Services;

public class InventoryService : IInventoryService {
    private IGameGui gameGui;
    private IDataManager dataManager;

    public InventoryService(IGameGui gameGui, IDataManager dataManager) {
        this.gameGui = gameGui;
        this.dataManager = dataManager;
    }

    public unsafe int GetItemCountInInventory(uint itemId) {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) {
            return 0;
        }

        return inventoryManager->GetInventoryItemCount(itemId);
    }

    public unsafe uint GetRetainerMarketItemPrice(int slotIndex) {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) {
            return 0u;
        }

        return (uint)inventoryManager->GetRetainerMarketPrice((short)slotIndex);
    }

    public unsafe IReadOnlyList<InventorySlotInfo> GetInventorySlots(InventoryType inventoryType) {
        var slots = new List<InventorySlotInfo>();
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) {
            return slots;
        }

        var container = inventoryManager->GetInventoryContainer(inventoryType);
        if (container == null) {
            return slots;
        }

        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);

            if (item == null || item->ItemId == 0u) {
                slots.Add(new InventorySlotInfo {
                    SlotIndex = (uint)i,
                    IsOccupied = false,
                    ItemId = 0u,
                    Quantity = 0u,
                    PricePerUnit = 0u
                });
            }
            else {
                uint price = 0u;
                if (inventoryType == InventoryType.RetainerMarket) {
                    price = (uint)inventoryManager->GetRetainerMarketPrice((short)i);
                }

                slots.Add(new InventorySlotInfo {
                    SlotIndex = (uint)i,
                    IsOccupied = true,
                    ItemId = item->ItemId,
                    Quantity = (uint)item->Quantity,
                    PricePerUnit = price
                });
            }
        }

        return slots;
    }

    public unsafe int GetUiIndexForRetainerMarketItem(int slotIndex) {
        // Retainer market UI rows natively map 1:1 with inventory slots 0-19.
        // Returning the exact slot index guarantees we click the correct physical item.
        return slotIndex;
    }
}