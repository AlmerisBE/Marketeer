using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.MarketPricing.Contracts;
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
    private IMarketPriceCacheService priceProvider;
    private IItemResolverService itemResolver;
    private IConfigurationService configService;
    private IObjectTable objectTable;
    private ILocalizationService localization;
    private IAddonLifecycle addonLifecycle;
    private ILoggerService logger;
    private IMarketListingProvider listingProvider;
    private IMarketListingTrackerService listingTracker;
    private IRetainerGuidanceService guidanceService;
    private IKeyState keyState;

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
        IItemResolverService itemResolver,
        IConfigurationService configService,
        IObjectTable objectTable,
        ILocalizationService localization,
        IAddonLifecycle addonLifecycle,
        ILoggerService logger,
        IMarketListingProvider listingProvider,
        IMarketListingTrackerService listingTracker,
        IRetainerGuidanceService guidanceService,
        IKeyState keyState) {

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
        this.listingTracker = listingTracker;
        this.guidanceService = guidanceService;
        this.keyState = keyState;

        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContextMenu", this.OnContextMenuSetup);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void TriggerAdjustment() {
        if (this.IsActive) return;

        this.IsActive = true;
        this.isFetchingPrice = false;
        this.sequenceStartTime = DateTime.Now;
        this.lastActionTime = DateTime.Now;
        this.searchResultOpenTime = DateTime.MinValue;
        this.priceFetchTask = null;
        this.logger.Info("[HybridAutomation] Event-driven sequence armed by Click Interceptor.");

        if (this.uiInteraction.IsAddonReady("ContextMenu")) this.HandleContextMenu();
    }

    private void HandleContextMenu() {
        this.logger.Debug("[HybridAutomation] Handling ContextMenu for RetainerSell adjustment.");
        var adjustText = this.localization.Translate("RetainerMenu_AdjustPrice");
        var index = this.uiInteraction.GetContextMenuItemIndex(adjustText);
        this.logger.Debug($"[HybridAutomation] ContextMenu item index for '{adjustText}': {index}");

        if (index != -1) {
            this.logger.Debug($"[HybridAutomation] Found target menu option at index {index}.");
            this.uiInteraction.SelectContextMenuItem(index);
        }
        else {
            this.logger.Warning("[HybridAutomation] Target menu option not found. Safely aborting to prevent agent corruption.");
            this.Abort();
        }
    }

    private bool IsModifierPressed() {
        var modifiers = this.configService.GetConfig().AutoSellModifierKey;
        if (modifiers == ModifierKey.None) return false;

        bool requiresCtrl = modifiers.HasFlag(ModifierKey.Ctrl);
        bool requiresShift = modifiers.HasFlag(ModifierKey.Shift);
        bool requiresAlt = modifiers.HasFlag(ModifierKey.Alt);

        bool isCtrlPressed = this.keyState[VirtualKey.CONTROL];
        bool isShiftPressed = this.keyState[VirtualKey.SHIFT];
        bool isAltPressed = this.keyState[VirtualKey.MENU];

        return requiresCtrl == isCtrlPressed &&
               requiresShift == isShiftPressed &&
               requiresAlt == isAltPressed;
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

    private void OnContextMenuSetup(AddonEvent type, AddonArgs args) {
        this.EvaluateContextMenuSetup();
    }

    public void EvaluateContextMenuSetup() {
        if (!this.IsActive || this.currentListing != null) return;
        this.HandleContextMenu();
    }

    private void OnRetainerSellSetup(AddonEvent type, AddonArgs args) {
        this.EvaluateRetainerSellSetup(args.Addon.Address);
    }

    public void EvaluateRetainerSellSetup(nint addonAddress) {
        if (!this.IsActive && this.IsModifierPressed()) {
            this.IsActive = true;
            this.sequenceStartTime = DateTime.Now;
            this.logger.Info("[HybridAutomation] Modifier held during native RetainerSell open. Arming new sale sequence.");
        }

        if (!this.IsActive || this.isFetchingPrice) return;

        if (!this.uiInteraction.GetActiveRetainerSellItemData(out var texts, out var originalPrice)) {
            this.logger.Error("[HybridAutomation] Could not read item data from RetainerSell nodes.");
            this.Abort();
            return;
        }

        var activeListings = this.listingProvider.GetActiveRetainerListings();
        var matchedListing = activeListings.FirstOrDefault(l => {
            var normalizedName = System.Text.RegularExpressions.Regex.Replace(l.ItemName, @"\s+", " ").Trim();
            return texts.Any(t => string.Equals(t, normalizedName, StringComparison.InvariantCultureIgnoreCase)) && l.PricePerUnit == originalPrice;
        });

        if (matchedListing == null) {
            matchedListing = activeListings.FirstOrDefault(l => {
                var normalizedName = System.Text.RegularExpressions.Regex.Replace(l.ItemName, @"\s+", " ").Trim();
                return texts.Any(t => t.Contains(normalizedName, StringComparison.InvariantCultureIgnoreCase));
            });
        }

        if (matchedListing != null) {
            this.currentListing = matchedListing;
        }
        else {
            uint resolvedItemId = 0;
            string resolvedName = string.Empty;

            foreach (var text in texts) {
                var id = this.itemResolver.ResolveItemId(text);
                if (id > 0) {
                    resolvedItemId = id;
                    resolvedName = text;
                    break;
                }
            }

            if (resolvedItemId == 0) {
                this.logger.Error("[HybridAutomation] Could not resolve Item ID for new sale.");
                this.Abort();
                return;
            }

            this.currentListing = new TrackedListing {
                ItemId = resolvedItemId,
                ItemName = resolvedName,
                PricePerUnit = originalPrice,
                AssociatedRetainerId = this.listingProvider.GetActiveRetainerId() ?? 0
            };
            this.logger.Info($"[HybridAutomation] Initiating new sale fetch for {resolvedName} (ID: {resolvedItemId}).");
        }

        this.uiInteraction.OpenComparePrices(addonAddress);
        this.isFetchingPrice = true;
        this.lastActionTime = DateTime.Now;
        this.searchResultOpenTime = DateTime.MinValue;
        this.priceFetchTask = null;
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
                    // Retry clicking Compare Prices if it didn't open yet
                    if ((DateTime.Now - this.lastActionTime).TotalSeconds > 1.0 && this.uiInteraction.IsAddonReady("RetainerSell")) {
                        this.uiInteraction.OpenComparePrices();
                        this.lastActionTime = DateTime.Now;
                    }
                }
                else {
                    // Initialize the fallback timer exactly when the window becomes visible
                    if (this.searchResultOpenTime == DateTime.MinValue) {
                        this.searchResultOpenTime = DateTime.Now;
                    }
                    else if ((DateTime.Now - this.searchResultOpenTime).TotalSeconds > 2.5) {
                        var localPlayer = this.objectTable.LocalPlayer;
                        if (localPlayer != null && listing != null) {
                            this.logger.Warning("[HybridAutomation] Live scanner timed out or market is empty. Falling back to API.");
                            this.priceFetchTask = this.priceProvider.GetLowestPricesAsync(new[] { listing.ItemId }, localPlayer.CurrentWorld.RowId, true);
                        }
                        this.searchResultOpenTime = DateTime.MinValue; // Prevent re-triggering
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
        uint newPrice = listing.PricePerUnit;

        if (prices.Count > 0) {
            var lowest = prices.OrderBy(p => p.Price).First();
            var config = this.configService.GetConfig();
            var vendorPrice = this.itemResolver.ResolveVendorPrice(listing.ItemId);

            if (this.IsOurRetainer(lowest.RetainerName)) newPrice = lowest.Price;
            else if (config.EnforceVendorPriceMinimum && lowest.Price <= vendorPrice) newPrice = listing.PricePerUnit;
            else {
                newPrice = (uint)Math.Max(1, (int)lowest.Price - (int)config.UndercutAmount);
                if (config.EnforceVendorPriceMinimum && newPrice < vendorPrice) newPrice = vendorPrice;
            }
        }

        this.uiInteraction.CloseItemSearchResult();
        this.uiInteraction.SetPriceAndConfirm(newPrice);

        if (listing.AssociatedRetainerId != 0) {
            this.listingTracker.RegisterPriceUpdate(listing.AssociatedRetainerId, listing.ItemId, newPrice);
        }

        this.guidanceService.ClearInstruction();

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
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ContextMenu", this.OnContextMenuSetup);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}