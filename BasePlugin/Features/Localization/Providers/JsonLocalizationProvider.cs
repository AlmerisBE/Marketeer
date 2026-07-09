using BasePlugin.Features.Localization.Contracts;
using Dalamud.Game;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace BasePlugin.Features.Localization.Providers;

public abstract class JsonLocalizationProvider : ILocalizationProvider {
    // Defines the root namespace path for the feature's resources
    protected abstract string ResourceBasePath { get; }

    public IReadOnlyDictionary<ClientLanguage, Dictionary<string, string>> GetTranslations() {
        var assembly = Assembly.GetExecutingAssembly();
        var translations = new Dictionary<ClientLanguage, Dictionary<string, string>>();

        // Map expected file suffixes to Dalamud's language enum
        var languageMap = new Dictionary<string, ClientLanguage> {
            { "ja", ClientLanguage.Japanese },
            { "en", ClientLanguage.English },
            { "de", ClientLanguage.German },
            { "fr", ClientLanguage.French }
        };

        foreach (var lang in languageMap) {
            var resourceName = $"{this.ResourceBasePath}.{lang.Key}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);

            // If the specific language file doesn't exist, we gracefully skip it
            if (stream == null) {
                continue;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (parsed != null) {
                translations[lang.Value] = parsed;
            }
        }

        return translations;
    }
}