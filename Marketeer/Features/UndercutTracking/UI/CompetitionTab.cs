using Dalamud.Bindings.ImGui;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using System.Numerics;

namespace Marketeer.Features.UndercutTracking.UI;

public class CompetitionTab : IDashboardTab {
    private ICompetitionStateService competitionState;
    private ILocalizationService localizationService;

    public string Name => this.localizationService.Translate("Undercuts_TabName");

    // Positioned after Financials (Priority 30)
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

        if (ImGui.BeginTable("CompetitionTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableSetupColumn("Item Name");
            ImGui.TableSetupColumn("Retainer");
            ImGui.TableSetupColumn("Our Price");
            ImGui.TableSetupColumn("Server Lowest");
            ImGui.TableHeadersRow();

            foreach (var item in items) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(item.ItemName);

                ImGui.TableNextColumn();
                ImGui.Text(item.RetainerName);

                ImGui.TableNextColumn();
                // Draw our price in red to indicate it is higher
                ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), item.OurPrice.ToString("N0"));

                ImGui.TableNextColumn();
                // Draw the server lowest in green
                ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), item.ServerCheapestPrice.ToString("N0"));
            }

            ImGui.EndTable();
        }
    }
}