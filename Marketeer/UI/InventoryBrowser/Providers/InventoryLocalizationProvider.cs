using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.InventoryBrowser.Providers;

public class InventoryLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.API.InventoryTracking.Resources";
}