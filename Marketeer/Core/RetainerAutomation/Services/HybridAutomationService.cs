using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.RetainerAutomation.Services;

public class HybridAutomationService : IHybridAutomationService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private IMarketPriceCacheService priceProvider;
    private IObjectTable objectTable;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IMarketListingTrackerService listingTracker;
    private IRetainerGuidanceService guidanceService;
    private IPriceCalculationService priceCalculationService;
    private INotificationService notificationService;

    private TrackedListing? currentListing;
    private DateTime sequenceStartTime;
    private DateTime lastActionTime;
    private DateTime searchResultOpenTime;
    private Task<IReadOnlyList<LowestPriceResult>>? priceFetchTask;
    private bool isFetchingPrice;

    public bool IsActive { get; private set; }

    public HybridAutomationService(
        IFramework framework,
        IRetainerUiInteractionService uiInteraction,
        IMarketPriceCacheService priceProvider,
        IObjectTable objectTable,
        ILocalizationService localization,
        ILoggerService logger,
        IMarketListingTrackerService listingTracker,
        IRetainerGuidanceService guidanceService,
        IPriceCalculationService priceCalculationService,
        INotificationService notificationService) {

        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.priceProvider = priceProvider;
        this.objectTable = objectTable;
        this.localization = localization;
        this.logger = logger;
        this.listingTracker = listingTracker;
        this.guidanceService = guidanceService;
        this.priceCalculationService = priceCalculationService;
        this.notificationService = notificationService;

        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void StartPriceUpdate(TrackedListing listing, nint addonAddress) {
        if (this.IsActive) return;

        this.IsActive = true;
        this.currentListing = listing;
        this.isFetchingPrice = true;
        this.sequenceStartTime = DateTime.Now;
        this.lastActionTime = DateTime.Now;
        this.searchResultOpenTime = DateTime.MinValue;
        this.priceFetchTask = null;

        this.logger.Info($"[HybridAutomation] Initiating new sale fetch for {listing.ItemName} (ID: {listing.ItemId}).");
        this.uiInteraction.OpenComparePrices(addonAddress);
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        var listing = this.currentListing;
        if (!this.IsActive || !this.isFetchingPrice || listing == null || this.priceFetchTask != null) return;

        if (updatedItemIds.Contains(listing.ItemId)) {
            var localPlayer = this.objectTable.LocalPlayer;
            if (localPlayer != null && localPlayer.CurrentWorld.RowId == worldId) {
                this.priceFetchTask = this.priceProvider.GetLowestPricesAsync(new[] { listing.ItemId }, worldId, false);
            }
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.IsActive) return;

        if ((DateTime.Now - this.sequenceStartTime).TotalSeconds > 15) {
            this.logger.Error("[HybridAutomation] Sequence timed out. Aborting.");
            this.Abort();
            return;
        }

        if (this.isFetchingPrice) {
            var fetchTask = this.priceFetchTask;
            var listing = this.currentListing;

            if (fetchTask == null) {
                bool isSearchResultReady = this.uiInteraction.IsAddonReady("ItemSearchResult");

                if (!isSearchResultReady) {
                    if ((DateTime.Now - this.lastActionTime).TotalSeconds > 1.0 && this.uiInteraction.IsAddonReady("RetainerSell")) {
                        this.uiInteraction.OpenComparePrices();
                        this.lastActionTime = DateTime.Now;
                    }
                }
                else {
                    if (this.searchResultOpenTime == DateTime.MinValue) {
                        this.searchResultOpenTime = DateTime.Now;
                    }
                    else if ((DateTime.Now - this.searchResultOpenTime).TotalSeconds > 2.5) {
                        var localPlayer = this.objectTable.LocalPlayer;
                        if (localPlayer != null && listing != null) {
                            this.logger.Warning("[HybridAutomation] Live scanner timed out or market is empty. Falling back to API.");
                            this.priceFetchTask = this.priceProvider.GetLowestPricesAsync(new[] { listing.ItemId }, localPlayer.CurrentWorld.RowId, true);
                        }
                        this.searchResultOpenTime = DateTime.MinValue;
                    }
                }
            }

            if (fetchTask != null && fetchTask.IsCompleted) this.ApplyPriceAndFinish();
        }
    }

    private void ApplyPriceAndFinish() {
        var listing = this.currentListing;
        var fetchTask = this.priceFetchTask;

        if (listing == null || fetchTask == null) return;

        this.isFetchingPrice = false;

        var prices = fetchTask.Result.Where(p => p.ItemId == listing.ItemId).ToList();
        var calcResult = this.priceCalculationService.CalculateTargetPrice(listing.ItemId, listing.PricePerUnit, prices);

        this.uiInteraction.CloseItemSearchResult();

        if (calcResult.Action == PricingAction.CancelListing) {
            this.logger.Info($"[HybridAutomation] Live fetch indicates loss. Keeping price to allow manual cancellation on next click.");
            this.uiInteraction.CloseUnexpectedWindows();

            var format = this.localization.Translate("Notification_Price_TooLow") ?? "Price too low for {0}. Click again to cancel.";
            this.notificationService.ShowWarning("Marketeer", string.Format(format, listing.ItemName));
        }
        else if (calcResult.Action == PricingAction.KeepPrice) {
            this.logger.Info($"[HybridAutomation] Target price is identical or restricted. Keeping current price.");
            this.uiInteraction.CloseUnexpectedWindows();

            var format = this.localization.Translate("Notification_Price_Kept") ?? "{0} price kept at {1:N0}g.";
            this.notificationService.ShowWarning("Marketeer", string.Format(format, listing.ItemName, calcResult.CalculatedPrice));
        }
        else {
            this.uiInteraction.SetPriceAndConfirm(calcResult.CalculatedPrice);
            if (listing.AssociatedRetainerId != 0) {
                this.listingTracker.RegisterPriceUpdate(listing.AssociatedRetainerId, listing.ItemId, calcResult.CalculatedPrice);
            }
            this.logger.Info($"[HybridAutomation] Price natively updated to {calcResult.CalculatedPrice}.");

            var format = this.localization.Translate("Notification_Price_Updated") ?? "{0} updated to {1:N0}g.";
            this.notificationService.ShowSuccess("Marketeer", string.Format(format, listing.ItemName, calcResult.CalculatedPrice));
        }

        this.guidanceService.ClearInstruction();
        this.IsActive = false;
        this.currentListing = null;
    }

    private void Abort() {
        this.IsActive = false;
        this.isFetchingPrice = false;
        this.currentListing = null;
        this.uiInteraction.CloseUnexpectedWindows();
    }

    public void Dispose() {
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}