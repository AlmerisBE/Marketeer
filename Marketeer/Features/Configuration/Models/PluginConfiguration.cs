using Dalamud.Configuration;
using Marketeer.Features.CharacterTracking.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool ExampleCheckbox { get; set; } = false;

    // New property to store our characters
    public List<TrackedCharacter> KnownCharacters { get; set; } = new();
}