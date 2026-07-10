using Dalamud.Bindings.ImGui;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Linq;

namespace Marketeer.Features.CharacterTracking.UI;

public class CharacterListWidget : IDashboardWidget {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private ILocalizationService localizationService;
    private IRetainerDataPresenter retainerDataPresenter;

    public string Name => this.localizationService.Translate("CharacterList_TabName");

    public CharacterListWidget(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        ILocalizationService localizationService,
        IRetainerDataPresenter retainerDataPresenter) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
        this.retainerDataPresenter = retainerDataPresenter;
    }

    public void Draw() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.Text(this.localizationService.Translate("CharacterList_NoCharacters"));
            return;
        }

        var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
        var forgetTooltip = this.localizationService.Translate("CharacterList_TooltipForget");
        var noRetainersLabel = this.localizationService.Translate("CharacterList_NoRetainers");
        var retainerColName = this.localizationService.Translate("CharacterList_RetainerColName");

        foreach (var character in characters) {
            var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
            var headerText = $"{character.Name} ({worldName})###{character.Name}_{character.HomeWorldId}";

            // Framed gives the "bubble" look. AllowOverlap lets us put the button on the same line.
            var treeFlags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.AllowOverlap;

            bool isExpanded = ImGui.TreeNodeEx(headerText, treeFlags);

            // Calculate position to align the Forget button to the right of the header
            var buttonWidth = ImGui.CalcTextSize(forgetLabel).X + 16f; // Add padding to text size
            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - buttonWidth);

            var isActive = this.trackerService.IsActiveCharacter(character.Name, character.HomeWorldId);
            if (isActive) {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button($"{forgetLabel}##btn_{character.Name}_{character.HomeWorldId}")) {
                this.trackerService.ForgetCharacter(character.Name, character.HomeWorldId);
            }

            if (isActive) {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    ImGui.SetTooltip(forgetTooltip);
                }
            }

            // Draw the retainers table inside the accordion if expanded
            if (isExpanded) {
                var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

                if (retainers.Count == 0) {
                    ImGui.TextDisabled(noRetainersLabel);
                }
                else {
                    if (ImGui.BeginTable($"RetainersTable_{character.Name}_{character.HomeWorldId}", 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
                        ImGui.TableSetupColumn(retainerColName);
                        ImGui.TableHeadersRow();

                        foreach (var retainer in retainers) {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text(retainer.Name);
                        }

                        ImGui.EndTable();
                    }
                }

                // Add some spacing after the table for visual breathing room
                ImGui.Spacing();
                ImGui.TreePop();
            }
        }
    }
}