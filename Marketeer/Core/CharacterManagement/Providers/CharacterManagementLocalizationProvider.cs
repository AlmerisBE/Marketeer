using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.CharacterManagement.Providers;

public class CharacterManagementLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.CharacterManagement.Resources";
}