using Dalamud.Bindings.ImGui;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketListings.Components;

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

        RetainerDetailsTablePresenter.DrawTable(listings, this.localizationService);
    }
}