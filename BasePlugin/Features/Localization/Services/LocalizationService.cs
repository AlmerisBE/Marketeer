using BasePlugin.Features.Localization.Contracts;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace BasePlugin.Features.Localization.Services;

public class LocalizationService : ILocalizationService {
    private IClientState clientState;
    private Dictionary<ClientLanguage, Dictionary<string, string>> translations;

    public LocalizationService(IClientState clientState, IEnumerable<ILocalizationProvider> providers) {
        this.clientState = clientState;

        // Initialize dictionaries for the 4 official languages
        this.translations = new Dictionary<ClientLanguage, Dictionary<string, string>> {
            { ClientLanguage.Japanese, new Dictionary<string, string>() },
            { ClientLanguage.English, new Dictionary<string, string>() },
            { ClientLanguage.German, new Dictionary<string, string>() },
            { ClientLanguage.French, new Dictionary<string, string>() }
        };

        // Aggregate translations from all feature modules
        foreach (var provider in providers) {
            foreach (var languagePair in provider.GetTranslations()) {
                if (!this.translations.ContainsKey(languagePair.Key)) {
                    continue;
                }

                foreach (var translationPair in languagePair.Value) {
                    this.translations[languagePair.Key][translationPair.Key] = translationPair.Value;
                }
            }
        }
    }

    public string Translate(string key) {
        var currentLanguage = this.clientState.ClientLanguage;

        // 1. Try to find the translation in the current UI language
        if (this.translations[currentLanguage].TryGetValue(key, out var translation)) {
            return translation;
        }

        // 2. Fallback to English if the translation is missing in the current language
        if (currentLanguage != ClientLanguage.English && this.translations[ClientLanguage.English].TryGetValue(key, out var fallback)) {
            return fallback;
        }

        // 3. Fallback to the raw key if totally missing
        return key;
    }

    public string Translate(string key, params object[] args) {
        return string.Format(this.Translate(key), args);
    }
}