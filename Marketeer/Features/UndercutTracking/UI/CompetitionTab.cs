using Dalamud.Bindings.ImGui;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using System.Linq;
using System.Numerics;

namespace Marketeer.Features.UndercutTracking.UI;

public class CompetitionTab : IDashboardTab {
    private ICompetitionStateService competitionState;
    private ILocalizationService localizationService;

    public string Name => this.localizationService.Translate("Undercuts_TabName");

    public int Priority => 40;

    public CompetitionTab(ICompetitionStateService competitionState, ILocalizationService localizationService) {
        this.competitionState = competitionState;
        this.localizationService = localizationService;
    }

    public void Draw() {
        var items = this.competitionState.GetUndercutItems();

        if (items.Count == 0) {
            ImGui.Text("All your listings are currently the cheapest on the server!");
            return;
        }

        var groupedByCharacter = items.GroupBy(u => u.CharacterName);

        foreach (var group in groupedByCharacter) {
            var charName = string.IsNullOrWhiteSpace(group.Key) ? "Unknown" : group.Key;

            if (ImGui.CollapsingHeader(charName, ImGuiTreeNodeFlags.DefaultOpen)) {
                if (ImGui.BeginTable($"CompetitionTable_{charName}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
                    ImGui.TableSetupColumn("Item Name");
                    ImGui.TableSetupColumn("Retainer");
                    ImGui.TableSetupColumn("Our Price");
                    ImGui.TableSetupColumn("Server Lowest");
                    ImGui.TableSetupColumn("Competitor");
                    ImGui.TableHeadersRow();

                    foreach (var item in group) {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(item.ItemName);

                        ImGui.TableNextColumn();
                        ImGui.Text(item.RetainerName);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), item.OurPrice.ToString("N0"));

                        ImGui.TableNextColumn();
                        ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), item.ServerCheapestPrice.ToString("N0"));

                        ImGui.TableNextColumn();
                        ImGui.Text(item.CompetitorName);
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}