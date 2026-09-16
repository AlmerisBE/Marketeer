using Dalamud.Bindings.ImGui;
using Marketeer.Core.MarketListings.Models;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.MarketListings.Components;

public static class RetainerDetailsTablePresenter {
    // FIX: Takes ListingDisplayData instead of TrackedListing to match the UI service layer return type
    public static void DrawTable(IReadOnlyList<ListingDisplayData> listings, ILocalizationService localizationService) {
        if (listings.Count == 0) {
            ImGui.TextDisabled("No data available.");
            return;
        }

        if (!ImGui.BeginTable("RetainerDetailsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable)) return;

        ImGui.TableSetupColumn(localizationService.Translate("RetainerDetails_ColName"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(localizationService.Translate("RetainerDetails_ColUnitPrice"), ImGuiTableColumnFlags.WidthFixed, 80f);
        ImGui.TableSetupColumn(localizationService.Translate("RetainerDetails_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 40f);
        ImGui.TableSetupColumn(localizationService.Translate("RetainerDetails_ColTotalPrice"), ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn(localizationService.Translate("RetainerDetails_ColTax"), ImGuiTableColumnFlags.WidthFixed, 70f);
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