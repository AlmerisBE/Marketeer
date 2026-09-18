using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
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

    private TrackedListing? currentListing;
    private int step;
    private DateTime nextActionAt;
    private DateTime sequenceStartTime;
    private Task<IReadOnlyList<LowestPriceResult>>? priceFetchTask;

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
        ILoggerService logger) {

        this.framework = framework;
        this.uiInteraction = uiInteraction;
        this.priceProvider = priceProvider;
        this.itemResolver = itemResolver;
        this.configService = configService;
        this.objectTable = objectTable;
        this.localization = localization;
        this.addonLifecycle = addonLifecycle;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerAdjustment(TrackedListing listing) {
        if (this.IsActive) return;

        this.currentListing = listing;
        this.IsActive = true;
        this.step = 1;
        this.sequenceStartTime = DateTime.Now;
        this.SetDelay(0.1);
        this.logger.Info($"[HybridAutomation] Sequence started for {listing.ItemName}");
    }

    private void OnRetainerSellSetup(AddonEvent type, AddonArgs args) {
        if (!this.IsActive || this.step != 2) return;

        this.logger.Info($"[HybridAutomation] RetainerSell PostSetup event received for address {args.Addon:X}. Triggering ComparePrices instantly.");
        this.uiInteraction.OpenComparePrices(args.Addon);
        this.step = 3;
        this.SetDelay(0.3);
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.IsActive || this.currentListing == null) return;

        if ((DateTime.Now - this.sequenceStartTime).TotalSeconds > 15) {
            this.logger.Error($"[HybridAutomation] Sequence timed out for {this.currentListing.ItemName}. Aborting.");
            this.Abort();
            return;
        }

        if (DateTime.Now < this.nextActionAt) return;

        switch (this.step) {
            case 1:
                if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                    var adjustText = this.localization.Translate("RetainerMenu_AdjustPrice");
                    var index = this.uiInteraction.GetContextMenuItemIndex(adjustText);

                    if (index == -1) index = 0;

                    this.uiInteraction.SelectContextMenuItem(index);
                    this.step = 2; // Waiting for OnRetainerSellSetup or fallback in step 2
                    this.SetDelay(0.2);
                }
                break;

            case 2:
                // Fallback in case PostSetup fired during context menu transition
                if (this.uiInteraction.IsAddonReady("RetainerSell")) {
                    this.logger.Debug("[HybridAutomation] RetainerSell ready via polling. Triggering ComparePrices.");
                    this.uiInteraction.OpenComparePrices();
                    this.step = 3;
                    this.SetDelay(0.3);
                }
                break;

            case 3:
                if (this.uiInteraction.IsAddonReady("ItemSearchResult")) {
                    var localPlayer = this.objectTable.LocalPlayer;
                    if (localPlayer == null) { this.Abort(); return; }

                    var worldId = localPlayer.CurrentWorld.RowId;
                    this.priceFetchTask = this.priceProvider.GetLowestPricesAsync(new[] { this.currentListing.ItemId }, worldId, false);
                    this.step++;
                    this.SetDelay(0.5);
                }
                else if (this.uiInteraction.IsAddonReady("RetainerSell")) {
                    this.logger.Debug("[HybridAutomation] ItemSearchResult not ready yet. Retrying OpenComparePrices...");
                    this.uiInteraction.OpenComparePrices();
                    this.SetDelay(0.4);
                }
                break;

            case 4:
                if (this.priceFetchTask == null || !this.priceFetchTask.IsCompleted) return;

                var prices = this.priceFetchTask.Result.Where(p => p.ItemId == this.currentListing.ItemId).ToList();
                uint newPrice = this.currentListing.PricePerUnit;

                if (prices.Count > 0) {
                    var lowest = prices.OrderBy(p => p.Price).First();
                    var config = this.configService.GetConfig();
                    var vendorPrice = this.itemResolver.ResolveVendorPrice(this.currentListing.ItemId);

                    if (this.IsOurRetainer(lowest.RetainerName)) {
                        newPrice = lowest.Price;
                    }
                    else if (config.EnforceVendorPriceMinimum && lowest.Price <= vendorPrice) {
                        newPrice = this.currentListing.PricePerUnit;
                    }
                    else {
                        newPrice = (uint)Math.Max(1, (int)lowest.Price - (int)config.UndercutAmount);
                        if (config.EnforceVendorPriceMinimum && newPrice < vendorPrice) newPrice = vendorPrice;
                    }
                }

                this.uiInteraction.SetPriceAndConfirm(newPrice);
                this.step++;
                this.SetDelay(2.0);
                break;

            case 5:
                this.uiInteraction.CloseItemSearchResult();
                this.IsActive = false;
                break;
        }
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
        this.uiInteraction.CloseUnexpectedWindows();
    }

    private void SetDelay(double seconds) {
        this.nextActionAt = DateTime.Now.AddSeconds(seconds);
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}