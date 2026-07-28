using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.InventoryTracking.Providers;

public class InventoryLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.InventoryTracking.Resources";
}