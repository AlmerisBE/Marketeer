using Dalamud.Bindings.ImGui;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.CompetitionTracking.UI;

public class CompetitionMenu : INavigationNode {
    private ICompetitionStateService competitionState;
    private ILocalizationService localizationService;
    private IPriceUpdateAutomationService priceUpdateService;

    public string GroupName => this.localizationService.Translate("Group_Market");
    public string Name => this.localizationService.Translate("Undercuts_TabName");
    public int Priority => 40;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionMenu(
        ICompetitionStateService competitionState,
        ILocalizationService localizationService,
        IPriceUpdateAutomationService priceUpdateService) {

        this.competitionState = competitionState;
        this.localizationService = localizationService;
        this.priceUpdateService = priceUpdateService;
    }

    public void DrawContent() {
        var items = this.competitionState.GetUndercutItems();

        if (items.Count == 0) {
            ImGui.TextUnformatted(this.localizationService.Translate("Competition_NoUndercuts"));
            return;
        }

        ImGui.TextWrapped(this.localizationService.Translate("Undercuts_UpdatePricesDescription"));
        ImGui.Spacing();

        var updateBtnText = this.localizationService.Translate("Undercuts_UpdatePricesButton");
        var buttonWidth = ImGui.CalcTextSize(updateBtnText).X + (ImGui.GetStyle().FramePadding.X * 2);

        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - buttonWidth);

        if (this.priceUpdateService.IsUpdating) {
            ImGui.BeginDisabled();
            ImGui.Button(this.localizationService.Translate("Undercuts_UpdatePricesActive"), new Vector2(buttonWidth, 24f));
            ImGui.EndDisabled();
        }
        else {
            if (ImGui.Button(updateBtnText, new Vector2(buttonWidth, 24f))) {
                this.priceUpdateService.TriggerPriceUpdate();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var groupedByCharacter = items.GroupBy(u => u.CharacterName);

        foreach (var group in groupedByCharacter) {
            var charName = string.IsNullOrWhiteSpace(group.Key) ? this.localizationService.Translate("Competition_UnknownCharacter") : group.Key;

            if (ImGui.CollapsingHeader(charName, ImGuiTreeNodeFlags.DefaultOpen)) {
                if (ImGui.BeginTable($"CompetitionTable_{charName}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColItemName"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColRetainer"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColOurPrice"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColServerLowest"));
                    ImGui.TableSetupColumn(this.localizationService.Translate("Competition_ColCompetitor"));
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
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}