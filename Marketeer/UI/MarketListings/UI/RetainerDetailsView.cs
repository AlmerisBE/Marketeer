using Dalamud.Bindings.ImGui;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketListings.Contracts;

namespace Marketeer.UI.MarketListings.UI;

public class RetainerDetailsView : IRetainerDetailsView {
    private IMarketListingTrackerService marketListingTrackerService;
    private ILocalizationService localizationService;

    public RetainerDetailsView(
        IMarketListingTrackerService marketListingTrackerService,
        ILocalizationService localizationService) {

        this.marketListingTrackerService = marketListingTrackerService;
        this.localizationService = localizationService;
    }

    public void Draw(RetainerDisplayData retainer) {
        ImGui.TextUnformatted(this.localizationService.Translate("RetainerDetails_Title", retainer.Name));
        ImGui.Separator();
        ImGui.Spacing();

        var listings = this.marketListingTrackerService.GetListingsForRetainer(retainer.RetainerId);

        if (listings.Count == 0) {
            ImGui.TextDisabled("No data available.");
            return;
        }

        if (ImGui.BeginTable("RetainerDetailsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("RetainerDetails_ColName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localizationService.Translate("RetainerDetails_ColUnitPrice"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localizationService.Translate("RetainerDetails_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 40f);
            ImGui.TableSetupColumn(this.localizationService.Translate("RetainerDetails_ColTotalPrice"), ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn(this.localizationService.Translate("RetainerDetails_ColTax"), ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableHeadersRow();

            foreach (var listing in listings) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(listing.ItemName);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{listing.PricePerUnit:N0}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(listing.Quantity.ToString());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{listing.TotalPrice:N0}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{listing.Tax:N0}");
            }

            ImGui.EndTable();
        }
    }
}