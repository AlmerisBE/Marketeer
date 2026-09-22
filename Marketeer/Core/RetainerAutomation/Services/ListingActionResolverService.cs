using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.SalesHistory.Contracts;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ListingActionResolverService : IListingActionResolverService {
    private IConfigurationService configService;
    private IMarketListingProvider listingProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private ILoggerService logger;

    public ListingActionResolverService(
        IConfigurationService configService,
        IMarketListingProvider listingProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        ILoggerService logger) {

        this.configService = configService;
        this.listingProvider = listingProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.logger = logger;
    }

    public ListingClickAction ResolveAction(uint itemId) {
        var config = this.configService.GetConfig();
        bool isRedLine = false;

        var undercut = this.competitionState.GetUndercutItems().FirstOrDefault(u => u.ItemId == itemId);
        if (undercut != null && undercut.SuggestedAction == PricingAction.CancelListing) {
            isRedLine = true;
        }
        else {
            uint vendorSellPrice = this.itemResolver.ResolveVendorPrice(itemId);
            var activeListings = this.listingProvider.GetActiveRetainerListings();
            var clickedListing = activeListings.FirstOrDefault(l => l.ItemId == itemId);

            if (config.EnforceVendorPriceMinimum && vendorSellPrice > 0 && clickedListing != null && clickedListing.PricePerUnit <= vendorSellPrice)
                isRedLine = true;
        }

        if (isRedLine && config.LossBehavior == MinimumPriceBehavior.CancelToInventory) {
            this.logger.Info($"[ListingActionResolver] Loss condition (Red Line) detected for Item ID {itemId}. Routing to cancellation.");
            return ListingClickAction.CancelListing;
        }

        this.logger.Info($"[ListingActionResolver] Standard listing detected for Item ID {itemId}. Routing to price update flow.");
        return ListingClickAction.UpdatePrice;
    }
}