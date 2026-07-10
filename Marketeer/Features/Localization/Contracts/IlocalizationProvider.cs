using Dalamud.Game;
using System.Collections.Generic;

namespace Marketeer.Features.Localization.Contracts;

public interface ILocalizationProvider {
    IReadOnlyDictionary<ClientLanguage, Dictionary<string, string>> GetTranslations();
}