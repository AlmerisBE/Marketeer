using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class ListingActionResolverService : IListingActionResolverService, IDisposable {
    private IConfigurationService configService;
    private IMarketListingProvider listingProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private ILoggerService logger;
    private IHybridAutomationService hybridAutomation;
    private IListingCancellationService cancellationService;

    public ListingActionResolverService(
        IConfigurationService configService,
        IMarketListingProvider listingProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        ILoggerService logger,
        IHybridAutomationService hybridAutomation,
        IListingCancellationService cancellationService) {

        this.configService = configService;
        this.listingProvider = listingProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.logger = logger;
        this.hybridAutomation = hybridAutomation;
        this.cancellationService = cancellationService;

        this.hybridAutomation.CancellationRequested += this.OnCancellationRequested;
    }

    private void OnCancellationRequested(uint itemId) {
        this.logger.Info($"[ListingActionResolver] Cancellation requested by Hybrid Automation for Item ID {itemId}.");
        this.cancellationService.TriggerCancellation(itemId);
    }

    public void ProcessListingClick(TrackedListing listing) {
        var action = this.ResolveAction(listing.ItemId);

        if (action == ListingClickAction.CancelListing) {
            this.logger.Info($"[ListingActionResolver] Routing {listing.ItemName} to cancellation.");
            this.cancellationService.TriggerCancellation(listing.ItemId);
        }
        else if (action == ListingClickAction.UpdatePrice) {
            this.logger.Info($"[ListingActionResolver] Routing {listing.ItemName} to price update.");
            this.hybridAutomation.StartPriceUpdate(listing);
        }
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

        return ListingClickAction.UpdatePrice;
    }

    public void Dispose() {
        this.hybridAutomation.CancellationRequested -= this.OnCancellationRequested;
    }
}