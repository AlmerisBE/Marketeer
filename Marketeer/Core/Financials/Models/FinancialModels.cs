using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.Financials.Models;

[Serializable]
public class RetainerMarketListingSaveData {
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }
    public DateTime ListingDate { get; set; }
}

[Serializable]
public class RetainerFinancialData {
    public ulong RetainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ulong GilHeld { get; set; }
    public Dictionary<int, RetainerMarketListingSaveData> MarketListings { get; set; } = new();

    public ulong TotalMarketValue => this.MarketListings.Values.Aggregate(0ul, (acc, listing) => acc + ((ulong)listing.PricePerUnit * listing.Quantity));
}

[Serializable]
public class CharacterFinancialData {
    public string CharacterName { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public string CompanyTag { get; set; } = string.Empty;
    public DateTime LastScanDate { get; set; }
    public ulong CharacterGil { get; set; }
    public Dictionary<ulong, RetainerFinancialData> Retainers { get; set; } = new();

    public string StorageKey => $"{this.CharacterName}_{this.HomeWorldId}";

    public ulong TotalGil => this.CharacterGil + this.Retainers.Values.Aggregate(0ul, (acc, ret) => acc + ret.GilHeld);

    public ulong TotalMarketValue => this.Retainers.Values.Aggregate(0ul, (acc, ret) => acc + ret.TotalMarketValue);
}

public class GlobalFinancialSummary {
    public List<CharacterFinancialData> Characters { get; set; } = new();
    public ulong GrandTotalGil { get; set; }
    public ulong GrandTotalMarketValue { get; set; }
}