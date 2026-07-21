using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.Financials.Providers;

public class FinancialsLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.Financials.Resources";
}