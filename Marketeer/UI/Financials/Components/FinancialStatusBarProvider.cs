using Dalamud.Bindings.ImGui;
using Marketeer.Core.Financials.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.Financials.Components;

public class FinancialStatusBarProvider : IStatusBarProvider {
    private IFinancialService financialService;
    private ILocalizationService localization;

    public int Priority => 10;

    public FinancialStatusBarProvider(IFinancialService financialService, ILocalizationService localization) {
        this.financialService = financialService;
        this.localization = localization;
    }

    public void Draw() {
        var summary = this.financialService.GetFinancialSummary();

        // On compte le nombre d'entrées dans le dictionnaire MarketListings
        var totalItems = summary.Characters.Sum(c => c.Retainers.Values.Sum(r => r.MarketListings.Count));

        var itemsText = this.localization.Translate("StatusBar_ItemsOnSale", totalItems);
        var valueText = this.localization.Translate("StatusBar_MarketValue", summary.GrandTotalMarketValue);
        var gilText = this.localization.Translate("StatusBar_TotalGil", summary.GrandTotalGil);

        ImGui.TextUnformatted(itemsText);
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();

        ImGui.TextUnformatted(valueText);
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();

        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), gilText);
    }
}