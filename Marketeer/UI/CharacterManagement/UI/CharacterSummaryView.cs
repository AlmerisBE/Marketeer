using Dalamud.Bindings.ImGui;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.UI.InventoryTracking.UI;
using System;
using System.Linq;

namespace Marketeer.UI.CharacterManagement.UI;

public class CharacterSummaryView : ICharacterSummaryView {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private ILocalizationService localizationService;
    private IRetainerDataPresenter retainerDataPresenter;
    private IMarketListingTrackerService marketListingTrackerService;
    private IDashboardNavigationService navigationService;
    private InventoryView inventoryView;

    public CharacterSummaryView(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        ILocalizationService localizationService,
        IRetainerDataPresenter retainerDataPresenter,
        IMarketListingTrackerService marketListingTrackerService,
        IDashboardNavigationService navigationService,
        InventoryView inventoryView) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.localizationService = localizationService;
        this.retainerDataPresenter = retainerDataPresenter;
        this.marketListingTrackerService = marketListingTrackerService;
        this.navigationService = navigationService;
        this.inventoryView = inventoryView;
    }

    public void Draw(TrackedCharacter character, Action<ulong> onRetainerSelected) {
        var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
        ImGui.TextUnformatted($"{character.Name} ({worldName})");
        ImGui.Separator();
        ImGui.Spacing();

        var forgetLabel = this.localizationService.Translate("CharacterList_BtnForget");
        var forgetTooltip = this.localizationService.Translate("CharacterList_TooltipForget");
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

        ImGui.Spacing();

        var retainers = this.retainerDataPresenter.GetRetainers(character.Name, character.HomeWorldId);

        if (retainers.Count == 0) {
            ImGui.TextDisabled(this.localizationService.Translate("CharacterList_NoRetainers"));
            return;
        }

        var retainerColName = this.localizationService.Translate("CharacterList_RetainerColName");
        var gilColName = this.localizationService.Translate("CharacterList_ColGil");
        var listingsColName = this.localizationService.Translate("CharacterList_ColListingsCount");
        var totalColName = this.localizationService.Translate("CharacterList_ColTotalValue");

        var onSaleButtonLabel = this.localizationService.Translate("CharacterList_BtnOnSale");
        var inventoryButtonLabel = this.localizationService.Translate("CharacterList_BtnInventory");

        var updateRequiredTooltip = this.localizationService.Translate("Dashboard_PriceUpdateRequired");
        var syncRequiredTooltip = this.localizationService.Translate("Dashboard_SyncRequired");

        var actionColumnWidth = ImGui.CalcTextSize(onSaleButtonLabel).X + ImGui.CalcTextSize(inventoryButtonLabel).X + (ImGui.GetStyle().FramePadding.X * 4) + ImGui.GetStyle().ItemSpacing.X;

        if (ImGui.BeginTable($"RetainersTable_{character.Name}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(retainerColName, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(gilColName, ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(listingsColName, ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn(totalColName, ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, actionColumnWidth);
            ImGui.TableHeadersRow();

            foreach (var retainer in retainers) {
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
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(isDesynced ? syncRequiredTooltip : updateRequiredTooltip);
                    }
                }
                else {
                    ImGui.TextUnformatted($"{totalValue:N0}");
                }

                ImGui.TableNextColumn();

                if (ImGui.Button($"{onSaleButtonLabel}##sale_{retainer.RetainerId}")) {
                    onRetainerSelected(retainer.RetainerId);
                }

                ImGui.SameLine();

                if (ImGui.Button($"{inventoryButtonLabel}##inv_{retainer.RetainerId}")) {
                    this.inventoryView.OpenForRetainer(retainer.RetainerId, retainer.Name);
                    this.navigationService.NavigateTo(this.inventoryView);
                }
            }

            ImGui.EndTable();
        }
    }
}