using Dalamud.Configuration;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.RetainerTracking.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool ExampleCheckbox { get; set; } = false;

    public List<TrackedCharacter> KnownCharacters { get; set; } = [];

    public List<TrackedRetainer> KnownRetainers { get; set; } = [];
}