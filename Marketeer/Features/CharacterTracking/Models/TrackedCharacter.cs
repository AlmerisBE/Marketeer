using System;

namespace Marketeer.Features.CharacterTracking.Models;

[Serializable]
public class TrackedCharacter {
    public string Name { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public string CompanyTag { get; set; } = string.Empty;
    public DateTime LastScanDate { get; set; }
    public int RetainerCount { get; set; }

    public override bool Equals(object? obj) {
        if (obj is TrackedCharacter other) {
            return this.Name == other.Name && this.HomeWorldId == other.HomeWorldId;
        }
        return false;
    }

    public override int GetHashCode() {
        return HashCode.Combine(this.Name, this.HomeWorldId);
    }
}