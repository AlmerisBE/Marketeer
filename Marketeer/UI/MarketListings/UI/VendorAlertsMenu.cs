using Dalamud.Bindings.ImGui;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.MarketListings.UI;

public class VendorAlertsMenu : INavigationNode {
    private IListingOptimizationService optimizationService;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Market");
    public string Name => this.localization.Translate("VendorAlerts_TabName");
    public int Priority => 50;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public VendorAlertsMenu(IListingOptimizationService optimizationService, ILocalizationService localization) {
        this.optimizationService = optimizationService;
        this.localization = localization;
    }

    public void DrawContent() {
        var listings = this.optimizationService.GetVendorPricedListings();

        if (listings.Count == 0) {
            ImGui.TextUnformatted(this.localization.Translate("VendorAlerts_NoIssues"));
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        ImGui.TextWrapped(this.localization.Translate("VendorAlerts_WarningMessage", listings.Count));
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTable("VendorAlertsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableSetupColumn(this.localization.Translate("VendorAlerts_ColItem"));
            ImGui.TableSetupColumn(this.localization.Translate("VendorAlerts_ColCharacter"));
            ImGui.TableSetupColumn(this.localization.Translate("VendorAlerts_ColRetainer"));
            ImGui.TableSetupColumn(this.localization.Translate("VendorAlerts_ColCurrentPrice"));
            ImGui.TableSetupColumn(this.localization.Translate("VendorAlerts_ColVendorPrice"));
            ImGui.TableHeadersRow();

            foreach (var listing in listings) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(listing.ItemName);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(listing.CharacterName);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(listing.RetainerName);

                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), $"{listing.Price:N0}");

                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), $"{listing.VendorPrice:N0}");
            }

            ImGui.EndTable();
        }
    }
}