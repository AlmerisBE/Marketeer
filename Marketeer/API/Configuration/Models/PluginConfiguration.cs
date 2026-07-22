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
    public List<SaleRecord> SalesHistory { get; set; } = new();

    public int UniversalisCacheMinutes { get; set; } = 30;
    public List<string> CompetitorWhitelist { get; set; } = new();

    public bool EnableAutomationDelay { get; set; } = false;
    public int AutomationDelayMin { get; set; } = 1;
    public int AutomationDelayMax { get; set; } = 3;
}