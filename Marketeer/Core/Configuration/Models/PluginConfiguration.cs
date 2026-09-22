using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.SalesHistory.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public Dictionary<string, CharacterFinancialData> FinancialRecords { get; set; } = [];
    public List<SaleRecord> SalesHistory { get; set; } = [];
    public Dictionary<string, InventorySnapshot> InventorySnapshots { get; set; } = [];
    public Dictionary<ulong, InventorySnapshot> RetainerInventorySnapshots { get; set; } = [];

    public int UniversalisCacheMinutes { get; set; } = 30;

    // Whitelist Settings
    public List<string> CompetitorWhitelist { get; set; } = [];
    public bool AutoWhitelistOwnRetainers { get; set; } = true;
    public WhitelistBehavior CompetitorWhitelistBehavior { get; set; } = WhitelistBehavior.Ignore;

    // Undercut Settings
    public UndercutMode UndercutMode { get; set; } = UndercutMode.Absolute;
    public uint UndercutAmount { get; set; } = 1;
    public double UndercutRelativePercentage { get; set; } = 1.0;

    // Minimum Price (Loss) Settings
    public bool EnforceVendorPriceMinimum { get; set; } = true;
    public MinimumPriceBehavior LossBehavior { get; set; } = MinimumPriceBehavior.KeepCurrentPrice;
    public InventoryPriority CancelInventoryPriority { get; set; } = InventoryPriority.PlayerFirst;

    // Fallback Settings (Empty Market)
    public FallbackPricingMode EmptyMarketFallbackMode { get; set; } = FallbackPricingMode.VendorSellMultiple;
    public double EmptyMarketFallbackMultiplier { get; set; } = 2.0;

    public bool EnableAutomationDelay { get; set; } = false;
    public int AutomationDelayMin { get; set; } = 1;
    public int AutomationDelayMax { get; set; } = 3;

    public bool EnableChatNotifications { get; set; } = true;
    public bool EnableDebugMode { get; set; } = false;

    public VirtualKey DashboardHotkey { get; set; } = VirtualKey.NO_KEY;
    public bool DashboardHotkeyCtrl { get; set; } = false;
    public bool DashboardHotkeyAlt { get; set; } = false;
    public bool DashboardHotkeyShift { get; set; } = false;

    public string SelectedTheme { get; set; } = "Default";

    public Dictionary<string, WatchedItem> WatchedItems { get; set; } = [];
    public Dictionary<uint, CraftingItemConfig> CraftingItems { get; set; } = [];

    public ModifierKey AutoSellModifierKey { get; set; } = ModifierKey.Shift;
}