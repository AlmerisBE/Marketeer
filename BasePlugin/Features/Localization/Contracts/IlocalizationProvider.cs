using Dalamud.Game;
using System.Collections.Generic;

namespace BasePlugin.Features.Localization.Contracts;

public interface ILocalizationProvider {
    IReadOnlyDictionary<ClientLanguage, Dictionary<string, string>> GetTranslations();
}