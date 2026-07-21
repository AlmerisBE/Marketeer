using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.Dashboard.Providers;

public class DashboardLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.Dashboard.Resources";
}