using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IHybridAutomationService hybridAutomation;
    private IListingCancellationService cancellationService;
    private IListingActionResolverService actionResolver;
    private IRetainerUiInteractionService uiInteraction;
    private IMarketListingProvider listingProvider;
    private IItemResolverService itemResolver;
    private IKeyState keyState;
    private IConfigurationService configService;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IFramework framework;

    private bool wantsToAutomate;
    private bool isEvaluating;
    private DateTime clickTime;

    public NativeListingClickInterceptorService(
        IAddonLifecycle addonLifecycle,
        IHybridAutomationService hybridAutomation,
        IListingCancellationService cancellationService,
        IListingActionResolverService actionResolver,
        IRetainerUiInteractionService uiInteraction,
        IMarketListingProvider listingProvider,
        IItemResolverService itemResolver,
        IKeyState keyState,
        IConfigurationService configService,
        ILocalizationService localization,
        ILoggerService logger,
        IFramework framework) {

        this.addonLifecycle = addonLifecycle;
        this.hybridAutomation = hybridAutomation;
        this.cancellationService = cancellationService;
        this.actionResolver = actionResolver;
        this.uiInteraction = uiInteraction;
        this.listingProvider = listingProvider;
        this.itemResolver = itemResolver;
        this.keyState = keyState;
        this.configService = configService;
        this.localization = localization;
        this.logger = logger;
        this.framework = framework;

        // Use PreReceiveEvent to guarantee capture before FFXIV steals focus to open the ContextMenu
        this.addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is AddonReceiveEventArgs receiveArgs && (int)receiveArgs.AtkEventType == 35) {
            if (this.IsModifierPressed()) {
                this.logger.Info("[ClickInterceptor] Modifier + Click detected on list. Arming extraction sequence.");
                this.wantsToAutomate = true;
                this.isEvaluating = false;
                this.clickTime = DateTime.Now;
            }
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.wantsToAutomate) return;

        if ((DateTime.Now - this.clickTime).TotalSeconds > 1.5) {
            this.logger.Warning("[ClickInterceptor] Extraction sequence timed out waiting for ContextMenu.");
            this.wantsToAutomate = false;
            return;
        }

        if (this.uiInteraction.IsAddonReady("ContextMenu")) {
            this.wantsToAutomate = false;
            this.isEvaluating = true;

            var adjustText = this.localization.Translate("RetainerMenu_AdjustPrice");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(adjustText);

            if (menuIndex != -1) {
                this.uiInteraction.SelectContextMenuItem(menuIndex);
            }
            else {
                this.logger.Warning("[ClickInterceptor] 'Adjust Price' option not found in context menu. Aborting sequence.");
                this.isEvaluating = false;
            }
        }
    }

    private void OnRetainerSellSetup(AddonEvent type, AddonArgs args) {
        bool triggeredManually = !this.isEvaluating && this.IsModifierPressed();

        if (!this.isEvaluating && !triggeredManually) return;

        this.isEvaluating = false;
        this.EvaluateRetainerSell(args.Addon);
    }

    public void EvaluateRetainerSell(nint addonAddress) {
        if (this.hybridAutomation.IsActive || this.cancellationService.IsActive) return;

        if (!this.uiInteraction.GetActiveRetainerSellItemData(out var texts, out var originalPrice)) {
            this.logger.Error("[ClickInterceptor] Could not read item data from RetainerSell nodes.");
            return;
        }

        var activeListings = this.listingProvider.GetActiveRetainerListings();
        var matchedListing = activeListings.FirstOrDefault(l => {
            var normalizedName = Regex.Replace(l.ItemName, @"\s+", " ").Trim();
            return texts.Any(t => string.Equals(t, normalizedName, StringComparison.InvariantCultureIgnoreCase)) && l.PricePerUnit == originalPrice;
        });

        if (matchedListing == null) {
            matchedListing = activeListings.FirstOrDefault(l => {
                var normalizedName = Regex.Replace(l.ItemName, @"\s+", " ").Trim();
                return texts.Any(t => t.Contains(normalizedName, StringComparison.InvariantCultureIgnoreCase));
            });
        }

        TrackedListing currentListing;
        if (matchedListing != null) {
            currentListing = matchedListing;
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
                this.logger.Error("[ClickInterceptor] Could not resolve Item ID for intercepted sale.");
                return;
            }

            currentListing = new TrackedListing {
                ItemId = resolvedItemId,
                ItemName = resolvedName,
                PricePerUnit = originalPrice,
                AssociatedRetainerId = this.listingProvider.GetActiveRetainerId() ?? 0
            };
        }

        var action = this.actionResolver.ResolveAction(currentListing.ItemId);

        if (action == ListingClickAction.CancelListing) {
            this.logger.Info($"[ClickInterceptor] Routing {currentListing.ItemName} to cancellation.");
            this.uiInteraction.CloseUnexpectedWindows();
            this.cancellationService.TriggerCancellation(currentListing.ItemId);
        }
        else if (action == ListingClickAction.UpdatePrice) {
            this.logger.Info($"[ClickInterceptor] Routing {currentListing.ItemName} to price update.");
            this.hybridAutomation.StartPriceUpdate(currentListing, addonAddress);
        }
    }

    private bool IsModifierPressed() {
        var modifiers = this.configService.GetConfig().AutoSellModifierKey;
        if (modifiers == ModifierKey.None) return true;

        bool requiresCtrl = modifiers.HasFlag(ModifierKey.Ctrl);
        bool requiresShift = modifiers.HasFlag(ModifierKey.Shift);
        bool requiresAlt = modifiers.HasFlag(ModifierKey.Alt);

        return requiresCtrl == this.keyState[VirtualKey.CONTROL] &&
               requiresShift == this.keyState[VirtualKey.SHIFT] &&
               requiresAlt == this.keyState[VirtualKey.MENU];
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", this.OnRetainerSellSetup);
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}