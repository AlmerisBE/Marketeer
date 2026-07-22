using Dalamud.Bindings.ImGui;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.UI.MarketListings.UI;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.CharacterManagement.UI;

public class CharacterListMenu : INavigationNode {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private ILocalizationService localizationService;
    private IRetainerDataPresenter retainerDataPresenter;
    private IMarketListingTrackerService marketListingTrackerService;
    private RetainerDetailsWindow retainerDetailsWindow;

    public string GroupName => string.Empty;
    public string Name => this.localizationService.Translate("CharacterList_TabName");
    public int Priority => 10;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CharacterListMenu(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        ILocalizationService localizationService,
        IRetainerDataPresenter retainerDataPresenter,
        IMarketListingTrackerService marketListingTrackerService,
        RetainerDetailsWindow retainerDetailsWindow) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
        this.retainerDataPresenter = retainerDataPresenter;
        this.marketListingTrackerService = marketListingTrackerService;
        this.retainerDetailsWindow = retainerDetailsWindow;
    }

    public void DrawContent() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.Text(this.localizationService.Translate("CharacterList_NoCharacters"));
            return;
        }

        var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
        var forgetTooltip = this.localizationService.Translate("CharacterList_TooltipForget");
        var noRetainersLabel = this.localizationService.Translate("CharacterList_NoRetainers");
        var retainerColName = this.localizationService.Translate("CharacterList_RetainerColName");
        var gilColName = this.localizationService.Translate("CharacterList_ColGil");
        var listingsColName = this.localizationService.Translate("CharacterList_ColListingsCount");
        var totalColName = this.localizationService.Translate("CharacterList_ColTotalValue");
        var detailsButtonLabel = this.localizationService.Translate("Dashboard_DetailsButton");
        var updateRequiredTooltip = this.localizationService.Translate("Dashboard_PriceUpdateRequired");
        var syncRequiredTooltip = this.localizationService.Translate("Dashboard_SyncRequired");

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

                    if (ImGui.BeginTable($"RetainersTable_{character.Name}_{character.HomeWorldId}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(fullWidth, 0))) {
                        ImGui.TableSetupColumn(retainerColName, ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn(gilColName, ImGuiTableColumnFlags.WidthFixed, 100f);
                        ImGui.TableSetupColumn(listingsColName, ImGuiTableColumnFlags.WidthFixed, 60f);
                        ImGui.TableSetupColumn(totalColName, ImGuiTableColumnFlags.WidthFixed, 100f);
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(detailsButtonLabel).X + 16f);
                        ImGui.TableHeadersRow();

                        foreach (var retainer in retainers) {
                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();
                            ImGui.Text(retainer.Name);

                            ImGui.TableNextColumn();
                            ImGui.Text($"{retainer.Gil:N0}");

                            var listings = this.marketListingTrackerService.GetListingsForRetainer(retainer.RetainerId);
                            var distinctItems = listings.Count;
                            var totalValue = listings.Sum(l => l.TotalPrice);

                            var needsPriceUpdate = distinctItems > 0 && listings.Any(l => l.PricePerUnit == 0);
                            var isDesynced = distinctItems != retainer.MarketItemCount;

                            ImGui.TableNextColumn();
                            ImGui.Text(retainer.MarketItemCount.ToString());

                            ImGui.TableNextColumn();
                            if (isDesynced || needsPriceUpdate) {
                                ImGui.TextDisabled($"{totalValue:N0} (!)");
                                if (ImGui.IsItemHovered()) {
                                    ImGui.SetTooltip(isDesynced ? syncRequiredTooltip : updateRequiredTooltip);
                                }
                            }
                            else {
                                ImGui.Text($"{totalValue:N0}");
                            }

                            ImGui.TableNextColumn();
                            if (ImGui.Button($"{detailsButtonLabel}##det_{retainer.RetainerId}")) {
                                this.retainerDetailsWindow.OpenForRetainer(retainer.RetainerId, retainer.Name);
                            }
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