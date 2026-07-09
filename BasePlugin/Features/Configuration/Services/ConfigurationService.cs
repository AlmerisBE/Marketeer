using BasePlugin.Features.Configuration.Contracts;
using BasePlugin.Features.Configuration.Models;
using Dalamud.Plugin;

namespace BasePlugin.Features.Configuration.Services;

public class ConfigurationService : IConfigurationService {
    private IDalamudPluginInterface pluginInterface;
    private PluginConfiguration config;

    public ConfigurationService(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;

        // Load existing config or create a new one
        this.config = this.pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
    }

    public PluginConfiguration GetConfig() => this.config;

    public void Save() {
        this.pluginInterface.SavePluginConfig(this.config);
    }
}