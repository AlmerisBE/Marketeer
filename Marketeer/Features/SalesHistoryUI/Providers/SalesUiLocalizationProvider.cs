using Marketeer.Features.Localization.Providers;

namespace Marketeer.Features.SalesHistoryUI.Providers;

public class SalesUiLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "Marketeer.Features.SalesHistoryUI.Resources";
}