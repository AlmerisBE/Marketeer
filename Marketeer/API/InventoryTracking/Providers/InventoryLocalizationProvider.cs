using Marketeer.UI.Localization.Providers;

namespace Marketeer.API.InventoryTracking.Providers;

public class InventoryLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.InventoryTracking.Resources";
}