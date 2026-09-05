using Marketeer.UI.Localization.Providers;

namespace Marketeer.Core.SalesHistory.Providers;

public class SalesHistoryLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Core.SalesHistory.Resources";
}