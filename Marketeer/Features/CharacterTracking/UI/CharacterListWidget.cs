using Dalamud.Bindings.ImGui;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using System.Linq;

namespace Marketeer.Features.CharacterTracking.UI;

public class CharacterListWidget : IDashboardWidget {
    private ICharacterTrackerService trackerService;

    public string Name => "Characters";

    public CharacterListWidget(ICharacterTrackerService trackerService) {
        this.trackerService = trackerService;
    }

    public void Draw() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.Text("No characters tracked yet. Log in to a character to start.");
            return;
        }

        if (ImGui.BeginTable("CharacterTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn("Character Name");
            ImGui.TableSetupColumn("Home World ID");
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            foreach (var character in characters) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(character.Name);

                ImGui.TableNextColumn();
                ImGui.Text(character.HomeWorldId.ToString());

                ImGui.TableNextColumn();

                // UI Logic: Disable the button if this is the active character
                var isActive = this.trackerService.IsActiveCharacter(character.Name, character.HomeWorldId);

                if (isActive) {
                    ImGui.BeginDisabled();
                }

                if (ImGui.Button($"Forget##{character.Name}_{character.HomeWorldId}")) {
                    this.trackerService.ForgetCharacter(character.Name, character.HomeWorldId);
                }

                if (isActive) {
                    ImGui.EndDisabled();
                    // Provide a tooltip so the user understands why the button is greyed out
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                        ImGui.SetTooltip("You cannot forget the currently logged-in character.");
                    }
                }
            }
            ImGui.EndTable();
        }
    }
}