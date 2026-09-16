using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketListings.Components;
using System.Numerics;

namespace Marketeer.UI.MarketListings.UI;

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
        if (!this.currentRetainerId.HasValue) return;

        var listings = this.marketListingTrackerService.GetListingsForRetainer(this.currentRetainerId.Value);
        RetainerDetailsTablePresenter.DrawTable(listings, this.localizationService);
    }
}