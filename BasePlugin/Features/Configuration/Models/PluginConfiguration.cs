using Dalamud.Configuration;
using System;

namespace BasePlugin.Features.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    // Ajoute ici toutes tes futures options de sauvegarde
    public bool ExampleCheckbox { get; set; } = false;
}