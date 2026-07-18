using System;
using System.Collections.Generic;

namespace Marketeer.Features.Financials.Models;

[Serializable]
public class RetainerMarketListingSaveData {
    public uint ItemId { get; set; }
    public uint Quantity { get; set; }
    public uint PricePerUnit { get; set; }
}

[Serializable]
public class RetainerFinancialData {
    public ulong RetainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ulong GilHeld { get; set; }
    public Dictionary<int, RetainerMarketListingSaveData> MarketListings { get; set; } = new();
}

[Serializable]
public class CharacterFinancialData {
    public string CharacterName { get; set; } = string.Empty;
    public uint HomeWorldId { get; set; }
    public Dictionary<ulong, RetainerFinancialData> Retainers { get; set; } = new();
}

public class GlobalFinancialSummary {
    public List<CharacterFinancialData> Characters { get; set; } = new();
    public ulong GrandTotalGil { get; set; }
    public ulong GrandTotalMarketValue { get; set; }
}