using Dalamud.Configuration;
using Marketeer.Features.Financials.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.Configuration.Models;

[Serializable]
public class PluginConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool ExampleCheckbox { get; set; } = false;

    // Uses a composite string key "CharacterName_HomeWorldId" for absolute compatibility
    public Dictionary<string, CharacterFinancialData> FinancialRecords { get; set; } = [];
}