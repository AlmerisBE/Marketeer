using Dalamud.Configuration;
using Marketeer.API.CraftingProfit.Models;
using Marketeer.API.Financials.Models;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.SalesHistory.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.API.Configuration.Models;

public enum WhitelistBehavior {
    Ignore,
    MatchPrice
}

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public Dictionary<string, CharacterFinancialData> FinancialRecords { get; set; } = [];
    public List<SaleRecord> SalesHistory { get; set; } = [];
    public Dictionary<string, InventorySnapshot> InventorySnapshots { get; set; } = [];
    public Dictionary<ulong, InventorySnapshot> RetainerInventorySnapshots { get; set; } = [];

    public int UniversalisCacheMinutes { get; set; } = 30;
    public List<string> CompetitorWhitelist { get; set; } = [];
    public bool AutoWhitelistOwnRetainers { get; set; } = true;
    public WhitelistBehavior CompetitorWhitelistBehavior { get; set; } = WhitelistBehavior.Ignore;

    public bool EnableAutomationDelay { get; set; } = false;
    public int AutomationDelayMin { get; set; } = 1;
    public int AutomationDelayMax { get; set; } = 3;

    public bool EnableChatNotifications { get; set; } = true;

    public Dictionary<string, WatchedItem> WatchedItems { get; set; } = [];
    public Dictionary<uint, CraftingItemConfig> CraftingItems { get; set; } = [];
}