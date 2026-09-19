using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.RetainerAutomation.Services;

public class HybridAutomationService : IHybridAutomationService, IDisposable {
    private IFramework framework;
    private IRetainerUiInteractionService uiInteraction;
    private IServerPriceProvider priceProvider;
    private IItemResolverService itemResolver;
    private IConfigurationService configService;
    private IObjectTable objectTable;
    private ILocalizationService localization;
    private IAddonLifecycle addonLifecycle;
    private ILoggerService logger;
    private IMarketListingProvider listingProvider;

    private TrackedListing? currentListing;
    private DateTime sequenceStartTime;
    private DateTime lastActionTime;
    private Task<IReadOnlyList<LowestPriceResult>>? priceFetchTask;
    private bool isFetchingPrice;

    public bool IsActive { get; private set; }

    public HybridAutomationService(
        IFramework framework,
        IRetainerUiInteractionService uiInteraction,
        IServerPriceProvider priceProvider,
        IItemResolverService itemResolver,
        IConfigurationService configService,
        IObjectTable objectTable,
        ILocalizationService localization,
        IAddonLifecycle addonLifecycle,
        ILoggerService logger,
        IMarketListingProvider listingProvider) {

        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.priceProvider = priceProvider;
        this.itemResolver = itemResolver;
        this.configService = configService;
        this.objectTable = objectTable;
        this.localization = localization;
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;
        this.listingProvider = listingProvider;

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContextMenu", this.OnContextMenuSetup);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "ItemSearchResult", this.OnItemSearchResultSetup);
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerAdjustment() {
        if (this.IsActive) return;

        this.IsActive = true;
        this.isFetchingPrice = false;
        this.sequenceStartTime = DateTime.Now;
        this.lastActionTime = DateTime.Now;
        this.logger.Info("[HybridAutomation] Event-driven sequence armed by Shift + left-click.");

        if (this.uiInteraction.IsAddonReady("ContextMenu")) this.HandleContextMenu();
    }

    private void HandleContextMenu() {
        this.logger.Debug("[HybridAutomation] ContextMenu active. Auto-selecting Adjust Price.");
        var adjustText = this.localization.Translate("RetainerMenu_AdjustPrice");
        var index = this.uiInteraction.GetContextMenuItemIndex(adjustText);

        if (index == -1) index = 0;

        this.uiInteraction.SelectContextMenuItem(index);
    }

    private void OnContextMenuSetup(AddonEvent type, AddonArgs args) {
        if (!this.IsActive || this.currentListing != null) return;
        this.HandleContextMenu();
    }

    private void OnRetainerSellSetup(AddonEvent type, AddonArgs args) {
        if (!this.IsActive || this.isFetchingPrice) return;

        if (!this.uiInteraction.GetActiveRetainerSellItemData(out var texts, out var originalPrice)) {
            this.logger.Error("[HybridAutomation] Could not read item data from RetainerSell nodes.");
            this.Abort();
            return;
        }

        var activeListings = this.listingProvider.GetActiveRetainerListings();

        this.currentListing = activeListings.FirstOrDefault(l =>
            texts.Any(t => string.Equals(t, l.ItemName, StringComparison.InvariantCultureIgnoreCase)) && l.PricePerUnit == originalPrice);

        if (this.currentListing == null) {
            this.currentListing = activeListings.FirstOrDefault(l =>
                texts.Any(t => t.Contains(l.ItemName, StringComparison.InvariantCultureIgnoreCase)));
        }

        if (this.currentListing == null) {
            this.logger.Error($"[HybridAutomation] Item not found. Scraped texts: {string.Join(", ", texts)}");
            this.Abort();
            return;
        }

        this.logger.Info($"[HybridAutomation] RetainerSell PostSetup for {this.currentListing.ItemName}. Triggering ComparePrices.");
        this.uiInteraction.OpenComparePrices(args.Addon.Address);
        this.isFetchingPrice = true;
        this.lastActionTime = DateTime.Now;
    }

    private void OnItemSearchResultSetup(AddonEvent type, AddonArgs args) {
        if (!this.IsActive || !this.isFetchingPrice || this.currentListing == null) return;

        this.logger.Info("[HybridAutomation] ItemSearchResult opened natively. Fetching Universalis prices.");
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) { this.Abort(); return; }

        this.priceFetchTask = this.priceProvider.GetLowestPricesAsync(new[] { this.currentListing.ItemId }, localPlayer.CurrentWorld.RowId, false);
        this.lastActionTime = DateTime.Now;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.IsActive) return;

        if ((DateTime.Now - this.sequenceStartTime).TotalSeconds > 15) {
            this.logger.Error("[HybridAutomation] Sequence timed out. Aborting.");
            this.Abort();
            return;
        }

        if (this.isFetchingPrice && this.priceFetchTask == null && (DateTime.Now - this.lastActionTime).TotalSeconds > 1.0) {
            if (!this.uiInteraction.IsAddonReady("ItemSearchResult") && this.uiInteraction.IsAddonReady("RetainerSell")) {
                this.logger.Debug("[HybridAutomation] ItemSearchResult missed setup. Retrying ComparePrices.");
                this.uiInteraction.OpenComparePrices();
                this.lastActionTime = DateTime.Now;
            }
        }

        if (this.isFetchingPrice && this.priceFetchTask != null && this.priceFetchTask.IsCompleted) {
            this.ApplyPriceAndFinish();
        }
    }

    private void ApplyPriceAndFinish() {
        if (this.currentListing == null || this.priceFetchTask == null) return;

        this.isFetchingPrice = false;

        var prices = this.priceFetchTask.Result.Where(p => p.ItemId == this.currentListing.ItemId).ToList();
        uint newPrice = this.currentListing.PricePerUnit;

        if (prices.Count > 0) {
            var lowest = prices.OrderBy(p => p.Price).First();
            var config = this.configService.GetConfig();
            var vendorPrice = this.itemResolver.ResolveVendorPrice(this.currentListing.ItemId);

            if (this.IsOurRetainer(lowest.RetainerName)) newPrice = lowest.Price;
            else if (config.EnforceVendorPriceMinimum && lowest.Price <= vendorPrice) newPrice = this.currentListing.PricePerUnit;
            else {
                newPrice = (uint)Math.Max(1, (int)lowest.Price - (int)config.UndercutAmount);
                if (config.EnforceVendorPriceMinimum && newPrice < vendorPrice) newPrice = vendorPrice;
            }
        }

        this.uiInteraction.CloseItemSearchResult();
        this.uiInteraction.SetPriceAndConfirm(newPrice);

        this.logger.Info($"[HybridAutomation] Price updated to {newPrice}. Sequence finished.");
        this.IsActive = false;
        this.currentListing = null;
    }

    private bool IsOurRetainer(string retainerName) {
        var config = this.configService.GetConfig();
        foreach (var cData in config.FinancialRecords.Values) {
            foreach (var rData in cData.Retainers.Values) {
                if (rData.Name == retainerName) return true;
            }
        }
        return false;
    }

    private void Abort() {
        this.IsActive = false;
        this.isFetchingPrice = false;
        this.currentListing = null;
        this.uiInteraction.CloseUnexpectedWindows();
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ContextMenu", this.OnContextMenuSetup);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ItemSearchResult", this.OnItemSearchResultSetup);
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}