using System;

namespace Marketeer.Core.CharacterManagement.Models;

[Serializable]
public class TrackedRetainer {
    public ulong RetainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint MarketItemCount { get; set; }
    public uint Gil { get; set; }

    public string AssociatedCharacterName { get; set; } = string.Empty;
    public uint AssociatedHomeWorldId { get; set; }

    public override bool Equals(object? obj) {
        if (obj is TrackedRetainer other) {
            return this.RetainerId == other.RetainerId;
        }
        return false;
    }

    public override int GetHashCode() {
        return this.RetainerId.GetHashCode();
    }
}