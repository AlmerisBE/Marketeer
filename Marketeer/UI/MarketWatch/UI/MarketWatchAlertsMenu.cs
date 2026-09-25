using Dalamud.Bindings.ImGui;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.MarketWatch.UI;

public class MarketWatchAlertsMenu : INavigationNode {
    private IMarketWatchAlertState alertState;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_MarketWatch") ?? "Market Watch";
    public string Name => this.localization.Translate("Menu_Opportunities") ?? "Opportunities";
    public int Priority => 51;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public MarketWatchAlertsMenu(IMarketWatchAlertState alertState, ILocalizationService localization) {
        this.alertState = alertState;
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("MarketWatch_AlertsHeader"));
        ImGui.Separator();
        ImGui.Spacing();

        if (this.alertState.LastUpdate != System.DateTime.MinValue) {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_LastCheck", this.alertState.LastUpdate.ToString("T")));
        }
        else {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_LastCheck", "-"));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var alerts = this.alertState.LatestAlerts
            .OrderBy(a => a.ItemName)
            .ThenByDescending(a => a.IsHighQuality)
            .ToList();

        if (alerts.Count == 0) {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_NoItems"));
            return;
        }

        if (ImGui.BeginTable("MarketWatchAlertsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColItem"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColAction"), ImGuiTableColumnFlags.WidthFixed, 130f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTarget"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColCurrent"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColSeller"), ImGuiTableColumnFlags.WidthFixed, 100f);
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
                ImGui.TextUnformatted(alert.TargetPrice.ToString("N0"));

                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.4f, 1.0f), alert.CurrentPrice.ToString("N0"));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(alert.RetainerName);
            }

            ImGui.EndTable();
        }
    }
}