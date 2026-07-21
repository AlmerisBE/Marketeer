using Marketeer.Core.Localization.Providers;

namespace Marketeer.UI.SalesHistory.Providers;

public class SalesHistoryLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.UI.SalesHistory.Resources";
}