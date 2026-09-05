using Dalamud.Game;
using System.Collections.Generic;

namespace Marketeer.UI.Localization.Contracts;

public interface ILocalizationProvider {
    IReadOnlyDictionary<ClientLanguage, Dictionary<string, string>> GetTranslations();
}