using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.Financials.Providers;

public class FinancialsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.Financials.Resources";
}