using Dalamud.Game;
using System.Collections.Generic;

namespace Marketeer.API.Localization.Contracts;

public interface ILocalizationProvider {
    IReadOnlyDictionary<ClientLanguage, Dictionary<string, string>> GetTranslations();
}