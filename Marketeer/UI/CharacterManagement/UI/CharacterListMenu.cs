using Dalamud.Bindings.ImGui;
using Marketeer.API.GameData.Contracts;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.InventoryBrowser.UI;
using Marketeer.UI.Localization.Contracts;
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
    private IDashboardNavigationService navigationService;
    private InventoryView inventoryView;

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
        RetainerDetailsWindow retainerDetailsWindow,
        IDashboardNavigationService navigationService,
        InventoryView inventoryView) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
        this.retainerDataPresenter = retainerDataPresenter;
        this.marketListingTrackerService = marketListingTrackerService;
        this.retainerDetailsWindow = retainerDetailsWindow;
        this.navigationService = navigationService;
        this.inventoryView = inventoryView;
    }

    public void DrawContent() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.TextUnformatted(this.localizationService.Translate("CharacterList_NoCharacters"));
            return;
        }

        foreach (var character in characters) {
            this.DrawCharacterCard(character);
        }
    }

    private void DrawCharacterCard(TrackedCharacter character) {
        var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
        var buttonWidth = ImGui.CalcTextSize(forgetLabel).X + (ImGui.GetStyle().FramePadding.X * 2);
        bool isExpanded = false;

        if (ImGui.BeginTable($"HeaderLayout_{character.Name}_{character.HomeWorldId}", 2, ImGuiTableFlags.None)) {
            ImGui.TableSetupColumn("Header", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, buttonWidth);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
            isExpanded = ImGui.TreeNodeEx($"{character.Name} ({worldName})###{character.Name}_{character.HomeWorldId}", ImGuiTreeNodeFlags.Framed);

            ImGui.TableNextColumn();
            var isActive = this.trackerService.IsActiveCharacter(character.Name, character.HomeWorldId);
            if (isActive) ImGui.BeginDisabled();

            if (ImGui.Button($"{forgetLabel}##btn_{character.Name}_{character.HomeWorldId}", new Vector2(-1, 0))) {
                this.trackerService.ForgetCharacter(character.Name, character.HomeWorldId);
            }

            if (isActive) {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    ImGui.SetTooltip(this.localizationService.Translate("CharacterList_TooltipForget"));
                }
            }

            ImGui.EndTable();
        }

        if (isExpanded) {
            float indent = ImGui.GetStyle().IndentSpacing;
            ImGui.Unindent(indent);
            this.DrawRetainersTable(character);
            ImGui.Indent(indent);
            ImGui.Spacing();
            ImGui.TreePop();
        }
    }

    private void DrawRetainersTable(TrackedCharacter character) {
        var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

        if (retainers.Count == 0) {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().CellPadding.X);
            ImGui.TextDisabled(this.localizationService.Translate("CharacterList_NoRetainers"));
            return;
        }

        var onSaleLabel = this.localizationService.Translate("CharacterList_BtnOnSale");
        var invLabel = this.localizationService.Translate("CharacterList_BtnInventory");
        var actionColumnWidth = ImGui.CalcTextSize(onSaleLabel).X + ImGui.CalcTextSize(invLabel).X + (ImGui.GetStyle().FramePadding.X * 4) + ImGui.GetStyle().ItemSpacing.X;
        float fullWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX();

        if (ImGui.BeginTable($"RetainersTable_{character.Name}_{character.HomeWorldId}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(fullWidth, 0))) {
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_RetainerColName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColGil"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColListingsCount"), ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn(this.localizationService.Translate("CharacterList_ColTotalValue"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, actionColumnWidth);
            ImGui.TableHeadersRow();

            foreach (var retainer in retainers) {
                this.DrawRetainerRow(retainer, onSaleLabel, invLabel);
            }
            ImGui.EndTable();
        }
    }

    private void DrawRetainerRow(RetainerDisplayData retainer, string onSaleLabel, string invLabel) {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(retainer.Name);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{retainer.Gil:N0}");

        var listings = this.marketListingTrackerService.GetListingsForRetainer(retainer.RetainerId);
        var distinctItems = listings.Count;
        var totalValue = listings.Sum(l => l.TotalPrice);
        var needsPriceUpdate = distinctItems > 0 && listings.Any(l => l.PricePerUnit == 0);
        var isDesynced = distinctItems != retainer.MarketItemCount;

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(retainer.MarketItemCount.ToString());
        ImGui.TableNextColumn();

        if (isDesynced || needsPriceUpdate) {
            ImGui.TextDisabled($"{totalValue:N0} (!)");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate(isDesynced ? "Dashboard_SyncRequired" : "Dashboard_PriceUpdateRequired"));
        }
        else ImGui.TextUnformatted($"{totalValue:N0}");

        ImGui.TableNextColumn();
        if (ImGui.Button($"{onSaleLabel}##det_{retainer.RetainerId}")) {
            this.retainerDetailsWindow.OpenForRetainer(retainer.RetainerId, retainer.Name);
        }
        ImGui.SameLine();
        if (ImGui.Button($"{invLabel}##inv_{retainer.RetainerId}")) {
            this.inventoryView.OpenForRetainer(retainer.RetainerId, retainer.Name);
            this.navigationService.NavigateTo(this.inventoryView);
        }
    }
}