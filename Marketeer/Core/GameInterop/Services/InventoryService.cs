using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.GameInterop.Services;

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
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) {
            return -1;
        }

        var container = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
        if (container == null) {
            return -1;
        }

        var targetSlot = container->GetInventorySlot(slotIndex);
        if (targetSlot == null || targetSlot->ItemId == 0) {
            return -1;
        }

        uint targetItemId = targetSlot->ItemId > 1000000u ? targetSlot->ItemId - 1000000u : targetSlot->ItemId;
        var sheet = this.dataManager.GetExcelSheet<Item>();

        string targetName = string.Empty;
        if (sheet != null && sheet.HasRow(targetItemId)) {
            targetName = sheet.GetRow(targetItemId).Name.ToString();
        }

        if (string.IsNullOrEmpty(targetName)) {
            return -1;
        }

        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) {
            return -1;
        }

        var addon = (AtkUnitBase*)addonPtr.Address;

        // Collect all active item names to identify the sequence in the UI data
        var activeNames = new HashSet<string>();
        for (int i = 0; i < container->Size; i++) {
            var item = container->GetInventorySlot(i);
            if (item != null && item->ItemId != 0) {
                uint id = item->ItemId > 1000000u ? item->ItemId - 1000000u : item->ItemId;
                if (sheet != null && sheet.HasRow(id)) {
                    string name = sheet.GetRow(id).Name.ToString();
                    if (!string.IsNullOrEmpty(name)) {
                        activeNames.Add(name);
                    }
                }
            }
        }

        int uiIndex = 0;

        // Iterate over the raw Addon memory to find the chronological UI order
        for (int i = 0; i < addon->AtkValuesCount; i++) {
            if (addon->AtkValues[i].Type == AtkValueType.String) {
                // Explicit cast to byte* to resolve CS0019 and CS0030 with HexaGen's CStringPointer
                var ptr = (byte*)addon->AtkValues[i].String;
                if (ptr != null) {
                    string val = MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue;
                    if (!string.IsNullOrEmpty(val)) {
                        // Remove potential HQ symbol from the UI string
                        string cleanVal = val.Replace("", "").Trim();

                        if (activeNames.Contains(cleanVal)) {
                            if (cleanVal == targetName) {
                                return uiIndex;
                            }

                            uiIndex++;
                        }
                    }
                }
            }
        }

        return -1;
    }
}