using Dalamud.Configuration;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.SalesHistoryTracking.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool ExampleCheckbox { get; set; } = false;

    public Dictionary<string, CharacterFinancialData> FinancialRecords { get; set; } = [];

    // Persistent storage for historical sales
    public List<SaleRecord> SalesHistory { get; set; } = new();
}