using Dalamud.Plugin;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketWatch.Models;
using System.Collections.Generic;

namespace Marketeer.Core.Configuration.Services;

public class ConfigurationService : IConfigurationService {
    private IDalamudPluginInterface pluginInterface;
    private PluginConfiguration config;

    public ConfigurationService(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;
        this.config = this.pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

        this.config.FinancialRecords ??= new();

        if (this.config.WatchedItems != null) {
            var rekeyed = new Dictionary<string, WatchedItem>();
            foreach (var kvp in this.config.WatchedItems) {
                var item = kvp.Value;
                rekeyed[item.Key] = item;
            }
            this.config.WatchedItems = rekeyed;
        }
        else {
            this.config.WatchedItems = new Dictionary<string, WatchedItem>();
        }
    }

    public PluginConfiguration GetConfig() => this.config;

    public void Save() {
        this.pluginInterface.SavePluginConfig(this.config);
    }
}