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
    private IMarketListingTrackerService marketListingTrackerService;

    public string Name => this.localizationService.Translate("CharacterList_TabName");

    public CharacterListWidget(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        ILocalizationService localizationService,
        IRetainerDataPresenter retainerDataPresenter,
        IMarketListingTrackerService marketListingTrackerService) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
        this.retainerDataPresenter = retainerDataPresenter;
        this.marketListingTrackerService = marketListingTrackerService;
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
        var listingsColName = this.localizationService.Translate("CharacterList_ColListingsCount");
        var totalColName = this.localizationService.Translate("CharacterList_ColTotalValue");

        var buttonWidth = ImGui.CalcTextSize(forgetLabel).X + (ImGui.GetStyle().FramePadding.X * 2);

        foreach (var character in characters) {
            bool isExpanded = false;

            if (ImGui.BeginTable($"HeaderLayout_{character.Name}_{character.HomeWorldId}", 2, ImGuiTableFlags.None)) {
                ImGui.TableSetupColumn("Header", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, buttonWidth);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
                var headerText = $"{character.Name} ({worldName})###{character.Name}_{character.HomeWorldId}";

                isExpanded = ImGui.TreeNodeEx(headerText, ImGuiTreeNodeFlags.Framed);

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

                ImGui.EndTable();
            }

            if (isExpanded) {
                float indent = ImGui.GetStyle().IndentSpacing;
                ImGui.Unindent(indent);

                var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

                if (retainers.Count == 0) {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
                    ImGui.TextDisabled(noRetainersLabel);
                }
                else {
                    float fullWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX();

                    // Added columns to support listings count and total value
                    if (ImGui.BeginTable($"RetainersTable_{character.Name}_{character.HomeWorldId}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(fullWidth, 0))) {
                        ImGui.TableSetupColumn(retainerColName, ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn(listingsColName, ImGuiTableColumnFlags.WidthFixed, 80f);
                        ImGui.TableSetupColumn(totalColName, ImGuiTableColumnFlags.WidthFixed, 120f);
                        ImGui.TableHeadersRow();

                        foreach (var retainer in retainers) {
                            ImGui.TableNextRow();

                            // Name
                            ImGui.TableNextColumn();
                            ImGui.Text(retainer.Name);

                            var listings = this.marketListingTrackerService.GetListingsForRetainer(retainer.RetainerId);
                            var distinctItems = listings.Count;
                            var totalValue = listings.Sum(l => (long)l.Quantity * l.PricePerUnit);

                            // Count
                            ImGui.TableNextColumn();
                            ImGui.Text(distinctItems.ToString());

                            // Total Value formatted nicely
                            ImGui.TableNextColumn();
                            ImGui.Text($"{totalValue:N0}");
                        }

                        ImGui.EndTable();
                    }
                }

                ImGui.Indent(indent);
                ImGui.Spacing();
                ImGui.TreePop();
            }
        }
    }
}