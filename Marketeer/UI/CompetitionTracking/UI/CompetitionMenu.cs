using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.CompetitionTracking.UI;

public class CompetitionMenu : INavigationNode {
    private ICompetitionStateService competitionState;
    private IWhitelistManagerService whitelistManager;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_ActiveSales") ?? "Sales & Competition";
    public string Name => this.localizationService.Translate("Menu_Competition") ?? "Competition";
    public int Priority => 20;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionMenu(
        ICompetitionStateService competitionState,
        IWhitelistManagerService whitelistManager,
        ILocalizationService localizationService) {

        this.competitionState = competitionState;
        this.whitelistManager = whitelistManager;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var items = this.competitionState.GetUndercutItems();

        if (items.Count == 0) {
            ImGui.TextUnformatted(this.localizationService.Translate("Competition_NoUndercuts"));
            return;
        }

        var groupedByCharacter = items.GroupBy(u => u.CharacterName);

        foreach (var group in groupedByCharacter) {
            var charName = string.IsNullOrWhiteSpace(group.Key) ? this.localizationService.Translate("Competition_UnknownCharacter") : group.Key;

            if (ImGui.CollapsingHeader(charName, ImGuiTreeNodeFlags.DefaultOpen)) {

                // Expanded to 7 columns to accommodate Average Price and Quick Actions
                if (ImGui.BeginTable($"CompetitionTable_{charName}", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColItemName"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColRetainer"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColOurPrice"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColServerLowest"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColCompetitor"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColAveragePrice") ?? "Average Price (Diff)");
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColActions") ?? "Actions", ImGuiTableColumnFlags.WidthFixed, 60f);

                    ImGui.TableHeadersRow();

                    foreach (var item in group) {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.ItemName);

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.RetainerName);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), item.OurPrice.ToString("N0"));

                        ImGui.TableNextColumn();
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), item.ServerCheapestPrice.ToString("N0"));

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.CompetitorName);

                        // Average Price and Relative Difference Calculation
                        ImGui.TableNextColumn();
                        string avgText = "-";
                        if (item.AverageMarketPrice > 0) {
                            float difference = ((float)item.ServerCheapestPrice - item.AverageMarketPrice) / item.AverageMarketPrice * 100f;
                            string sign = difference > 0 ? "+" : "";
                            avgText = $"{item.AverageMarketPrice:N0} ({sign}{difference:F1}%)";

                            // Color coding: Red if the cheapest price is severely crashing (>20% drop), otherwise standard text color
                            var color = difference < -20f ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.8f, 0.8f, 0.8f, 1f);
                            ImGui.TextColored(color, avgText);
                        }
                        else ImGui.TextUnformatted(avgText);

                        // Action Buttons Column
                        ImGui.TableNextColumn();
                        ImGui.PushFont(UiBuilder.IconFont);

                        // Add to Whitelist Button
                        if (ImGui.Button($"{FontAwesomeIcon.UserPlus.ToIconString()}##WL_{item.ItemId}_{item.CompetitorName}")) {
                            this.whitelistManager.AddToWhitelist(item.CompetitorName);
                        }

                        if (ImGui.IsItemHovered()) {
                            ImGui.PopFont();
                            ImGui.SetTooltip(this.localizationService.Translate("Tooltip_AddWhitelist") ?? "Add competitor to whitelist");
                            ImGui.PushFont(UiBuilder.IconFont);
                        }

                        ImGui.SameLine();

                        // Open Universalis Chart Button
                        if (ImGui.Button($"{FontAwesomeIcon.ChartLine.ToIconString()}##Chart_{item.ItemId}")) {
                            Dalamud.Utility.Util.OpenLink($"https://universalis.app/market/{item.ItemId}");
                        }

                        if (ImGui.IsItemHovered()) {
                            ImGui.PopFont();
                            ImGui.SetTooltip(this.localizationService.Translate("Tooltip_OpenChart") ?? "View price history on Universalis");
                            ImGui.PushFont(UiBuilder.IconFont);
                        }

                        ImGui.PopFont();
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}