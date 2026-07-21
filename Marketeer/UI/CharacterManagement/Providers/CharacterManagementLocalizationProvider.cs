using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.CharacterManagement.Providers;

public class CharacterManagementLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.CharacterTracking.Resources";
}