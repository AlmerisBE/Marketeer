using Marketeer.UI.Localization.Providers;

namespace Marketeer.UI.Dashboard.Providers;

public class DashboardLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.Dashboard.Resources";
}