using Dalamud.Bindings.ImGui;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Linq;

namespace Marketeer.Features.Dashboard.UI;

public class CharacterSummaryView : ICharacterSummaryView {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private ILocalizationService localizationService;
    private IRetainerDataPresenter retainerDataPresenter;
    private IMarketListingTrackerService marketListingTrackerService;

    public CharacterSummaryView(
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

    public void Draw(TrackedCharacter character) {
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
        var updateRequiredTooltip = this.localizationService.Translate("Dashboard_PriceUpdateRequired");
        var syncRequiredTooltip = this.localizationService.Translate("Dashboard_SyncRequired");

        if (ImGui.BeginTable($"RetainersTable_{character.Name}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(retainerColName, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(gilColName, ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(listingsColName, ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn(totalColName, ImGuiTableColumnFlags.WidthFixed, 100f);
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
            }

            ImGui.EndTable();
        }
    }
}