using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.Financials.Providers;

public class FinancialsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.Financials.Resources";
}