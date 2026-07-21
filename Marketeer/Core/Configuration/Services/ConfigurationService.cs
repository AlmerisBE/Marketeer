using Dalamud.Plugin;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;

namespace Marketeer.Core.Configuration.Services;

public class ConfigurationService : IConfigurationService {
    private IDalamudPluginInterface pluginInterface;
    private PluginConfiguration config;

    public ConfigurationService(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;
        this.config = this.pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

        // Guard against null collections when loading old configuration files
        this.config.FinancialRecords ??= new();
    }

    public PluginConfiguration GetConfig() => this.config;

    public void Save() {
        this.pluginInterface.SavePluginConfig(this.config);
    }
}