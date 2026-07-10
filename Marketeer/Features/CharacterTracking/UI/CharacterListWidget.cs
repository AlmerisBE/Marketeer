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
        IRetainerDataPresenter retainerDataPresenter) { // Dependency Injected here

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

        if (ImGui.BeginTable("CharacterTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColName"));
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColWorld"));
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColActions"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
            var forgetTooltip = this.localizationService.Translate("CharacterList_TooltipForget");
            var noRetainersLabel = this.localizationService.Translate("CharacterList_NoRetainers");

            foreach (var character in characters) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                // Using TreeNodeEx to create the accordion effect over the entire row width
                bool isExpanded = ImGui.TreeNodeEx($"{character.Name}##{character.HomeWorldId}", ImGuiTreeNodeFlags.SpanFullWidth);

                ImGui.TableNextColumn();
                var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
                ImGui.Text(worldName);

                ImGui.TableNextColumn();

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

                // If the user expanded the character accordion, query and draw the retainers
                if (isExpanded) {
                    var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

                    if (retainers.Count == 0) {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text($"   {noRetainersLabel}");
                        ImGui.TableNextColumn(); // Empty
                        ImGui.TableNextColumn(); // Empty
                    }
                    else {
                        foreach (var retainer in retainers) {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text($"   - {retainer.Name}");
                            ImGui.TableNextColumn(); // Keep empty for visual hierarchy
                            ImGui.TableNextColumn(); // Keep empty for visual hierarchy
                        }
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.EndTable();
        }
    }
}