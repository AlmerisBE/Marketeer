using Dalamud.Configuration;
using Marketeer.API.Financials.Models;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.API.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public Dictionary<string, CharacterFinancialData> FinancialRecords { get; set; } = [];

    // Persistent storage for historical sales
    public List<SaleRecord> SalesHistory { get; set; } = new();
}