using FFXIVClientStructs.FFXIV.Client.Game;

namespace Marketeer.API.RetainerAutomation.Models;

public class TransferredItemStack {
    public InventoryType Bag { get; set; }
    public uint SlotIndex { get; set; }
    public uint Quantity { get; set; }
}