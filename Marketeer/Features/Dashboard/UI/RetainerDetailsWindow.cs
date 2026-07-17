using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class RetainerDetailsWindow : Window {
    private IMarketListingTrackerService marketListingTrackerService;
    private ILocalizationService localizationService;

    private ulong? currentRetainerId;
    private string currentRetainerName = string.Empty;

    public RetainerDetailsWindow(
        IMarketListingTrackerService marketListingTrackerService,
        ILocalizationService localizationService)
        : base("Retainer Details", ImGuiWindowFlags.None) {

        this.marketListingTrackerService = marketListingTrackerService;
        this.localizationService = localizationService;

        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void OpenForRetainer(ulong retainerId, string retainerName) {
        this.currentRetainerId = retainerId;
        this.currentRetainerName = retainerName;
        this.WindowName = this.localizationService.Translate("RetainerDetails_Title", retainerName);
        this.IsOpen = true;
    }

    public override void Draw() {
        if (!this.currentRetainerId.HasValue) {
            return;
        }

        var listings = this.marketListingTrackerService.GetListingsForRetainer(this.currentRetainerId.Value);

        if (listings.Count == 0) {
            ImGui.Text("No data available.");
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
                ImGui.Text(listing.ItemName);

                ImGui.TableNextColumn();
                ImGui.Text($"{listing.PricePerUnit:N0}");

                ImGui.TableNextColumn();
                ImGui.Text(listing.Quantity.ToString());

                ImGui.TableNextColumn();
                ImGui.Text($"{listing.TotalPrice:N0}");

                ImGui.TableNextColumn();
                ImGui.Text($"{listing.Tax:N0}");
            }

            ImGui.EndTable();
        }
    }
}