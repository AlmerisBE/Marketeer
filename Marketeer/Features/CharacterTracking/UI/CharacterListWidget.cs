using Dalamud.Bindings.ImGui;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Linq;
using System.Numerics;

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

        var buttonWidth = ImGui.CalcTextSize(forgetLabel).X + (ImGui.GetStyle().FramePadding.X * 2);

        if (ImGui.BeginTable("CharacterLayoutTable", 2, ImGuiTableFlags.None)) {
            ImGui.TableSetupColumn("Header", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, buttonWidth);

            foreach (var character in characters) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
                var headerText = $"{character.Name} ({worldName})###{character.Name}_{character.HomeWorldId}";

                var treeFlags = ImGuiTreeNodeFlags.Framed;
                bool isExpanded = ImGui.TreeNodeEx(headerText, treeFlags);

                ImGui.TableNextColumn();
                var isActive = this.trackerService.IsActiveCharacter(character.Name, character.HomeWorldId);
                if (isActive) {
                    ImGui.BeginDisabled();
                }

                if (ImGui.Button($"{forgetLabel}##btn_{character.Name}_{character.HomeWorldId}", new Vector2(-1, 0))) {
                    this.trackerService.ForgetCharacter(character.Name, character.HomeWorldId);
                }

                if (isActive) {
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                        ImGui.SetTooltip(forgetTooltip);
                    }
                }

                if (isExpanded) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    float indent = ImGui.GetStyle().IndentSpacing;
                    ImGui.Unindent(indent);

                    var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

                    if (retainers.Count == 0) {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
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

                    ImGui.Indent(indent);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }
            }
            ImGui.EndTable();
        }
    }
}