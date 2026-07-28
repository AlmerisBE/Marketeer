using System;
using System.Collections.Generic;

namespace Marketeer.API.InventoryTracking.Models;

[Serializable]
public class InventorySnapshot {
    public string CharacterName { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<TrackedItem> Items { get; set; } = new();
}