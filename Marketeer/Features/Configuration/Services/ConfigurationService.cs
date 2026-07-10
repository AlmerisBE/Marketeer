using Dalamud.Plugin;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Configuration.Models;
using System.Collections.Generic;

namespace Marketeer.Features.Configuration.Services;

public class ConfigurationService : IConfigurationService {
    private IDalamudPluginInterface pluginInterface;
    private PluginConfiguration config;

    public ConfigurationService(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;

        // Load existing config or create a new one
        this.config = this.pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

        // Guard against null collections when loading old configuration files
        this.config.KnownCharacters ??= new List<TrackedCharacter>();
    }

    public PluginConfiguration GetConfig() => this.config;

    public void Save() {
        this.pluginInterface.SavePluginConfig(this.config);
    }
}