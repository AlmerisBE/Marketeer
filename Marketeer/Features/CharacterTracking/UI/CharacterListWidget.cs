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

    public string Name => this.localizationService.Translate("CharacterList_TabName");

    public CharacterListWidget(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        ILocalizationService localizationService) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
    }

    public void Draw() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.Text(this.localizationService.Translate("CharacterList_NoCharacters"));
            return;
        }

        if (ImGui.BeginTable("CharacterTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColName"));
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColWorld"));
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColActions"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
            var forgetTooltip = this.localizationService.Translate("CharacterList_TooltipForget");

            foreach (var character in characters) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(character.Name);

                ImGui.TableNextColumn();
                var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
                ImGui.Text(worldName);

                ImGui.TableNextColumn();

                var isActive = this.trackerService.IsActiveCharacter(character.Name, character.HomeWorldId);
                if (isActive) {
                    ImGui.BeginDisabled();
                }

                if (ImGui.Button($"{forgetLabel}##{character.Name}_{character.HomeWorldId}")) {
                    this.trackerService.ForgetCharacter(character.Name, character.HomeWorldId);
                }

                if (isActive) {
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                        ImGui.SetTooltip(forgetTooltip);
                    }
                }
            }
            ImGui.EndTable();
        }
    }
}