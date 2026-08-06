using Dalamud.Bindings.ImGui;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Marketeer.UI.MarketWatch.UI;

public class MarketWatchAlertsMenu : INavigationNode {
    private IMarketWatchAlertState alertState;
    private IMarketWatchAnalysisService analysisService;
    private ILocalizationService localization;

    private bool isAnalyzing;

    public string GroupName => this.localization.Translate("Group_MarketWatch");
    public string Name => this.localization.Translate("MarketWatch_AlertsTabName");
    public int Priority => 51;
    public bool HasContent => true;
    public bool DefaultExpanded => true;

    public MarketWatchAlertsMenu(
        IMarketWatchAlertState alertState,
        IMarketWatchAnalysisService analysisService,
        ILocalizationService localization) {

        this.alertState = alertState;
        this.analysisService = analysisService;
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("MarketWatch_AlertsHeader"));
        ImGui.Separator();
        ImGui.Spacing();

        if (this.alertState.LastUpdate != System.DateTime.MinValue) {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_LastUpdate", this.alertState.LastUpdate.ToString("T")));
            ImGui.SameLine();
        }

        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 120f);
        if (this.isAnalyzing) {
            ImGui.BeginDisabled();
            ImGui.Button(this.localization.Translate("MarketWatch_BtnScanning"), new Vector2(120f, 0));
            ImGui.EndDisabled();
        }
        else {
            if (ImGui.Button(this.localization.Translate("MarketWatch_BtnScanNow"), new Vector2(120f, 0))) {
                _ = this.TriggerManualAnalysisAsync();
            }
        }

        ImGui.Spacing();

        var alerts = this.alertState.LatestAlerts;

        if (alerts.Count == 0) {
            ImGui.TextUnformatted(this.localization.Translate("MarketWatch_NoAlerts"));
            return;
        }

        if (ImGui.BeginTable("MarketWatchAlertsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColItem"));
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColAction"));
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTargetPrice"));
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColCurrentPrice"));
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColSeller"));
            ImGui.TableHeadersRow();

            foreach (var alert in alerts) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                string hqSymbol = alert.IsHighQuality ? " \uE03C" : "";
                ImGui.TextUnformatted($"{alert.ItemName}{hqSymbol}");

                ImGui.TableNextColumn();
                if (alert.AlertType == MarketWatchAlertType.BuyTargetReached) {
                    ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), this.localization.Translate("MarketWatch_ActionBuy"));
                }
                else {
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.4f, 1.0f), this.localization.Translate("MarketWatch_ActionSell"));
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{alert.TargetPrice:N0}");

                ImGui.TableNextColumn();
                // Surligner le prix si l'écart est très intéressant
                bool isExceptionalDeal = (alert.AlertType == MarketWatchAlertType.BuyTargetReached && alert.CurrentPrice < alert.TargetPrice * 0.8) ||
                                         (alert.AlertType == MarketWatchAlertType.SellTargetReached && alert.CurrentPrice > alert.TargetPrice * 1.2);

                if (isExceptionalDeal) {
                    ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), $"{alert.CurrentPrice:N0}");
                }
                else {
                    ImGui.TextUnformatted($"{alert.CurrentPrice:N0}");
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(alert.RetainerName);
            }

            ImGui.EndTable();
        }
    }

    private async Task TriggerManualAnalysisAsync() {
        this.isAnalyzing = true;
        try {
            var newAlerts = await this.analysisService.AnalyzeMarketAsync(bypassCache: true);
            this.alertState.UpdateAlerts(newAlerts);
        }
        finally {
            this.isAnalyzing = false;
        }
    }
}