using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.Dashboard.Providers;

public class DashboardLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.Dashboard.Resources";
}