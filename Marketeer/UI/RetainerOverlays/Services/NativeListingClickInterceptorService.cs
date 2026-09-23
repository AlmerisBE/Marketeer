using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Linq;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IContextMenu contextMenu;
    private IListingActionResolverService actionResolver;
    private IMarketListingProvider listingProvider;
    private IRetainerUiInteractionService uiInteraction;
    private IItemResolverService itemResolver;
    private ILocalizationService localization;
    private IGameGui gameGui;
    private IKeyState keyState;
    private IConfigurationService configService;
    private ILoggerService logger;
    private IHybridAutomationService hybridAutomation;
    private IListingCancellationService cancellationService;
    private IFramework framework;

    private bool wantsToAutomateContextMenu;
    private bool isInventorySale;
    private bool wasRetainerSellVisible;
    private bool isAwaitingRetainerSellData;
    private TrackedListing? pendingListing;
    private DateTime clickTime;
    private DateTime retainerSellOpenTime;
    private ulong cachedHoverItemId;

    public NativeListingClickInterceptorService(
        IContextMenu contextMenu,
        IListingActionResolverService actionResolver,
        IMarketListingProvider listingProvider,
        IRetainerUiInteractionService uiInteraction,
        IItemResolverService itemResolver,
        ILocalizationService localization,
        IGameGui gameGui,
        IKeyState keyState,
        IConfigurationService configService,
        ILoggerService logger,
        IHybridAutomationService hybridAutomation,
        IListingCancellationService cancellationService,
        IFramework framework) {

        this.contextMenu = contextMenu;
        this.actionResolver = actionResolver;
        this.listingProvider = listingProvider;
        this.uiInteraction = uiInteraction;
        this.itemResolver = itemResolver;
        this.localization = localization;
        this.gameGui = gameGui;
        this.keyState = keyState;
        this.configService = configService;
        this.logger = logger;
        this.hybridAutomation = hybridAutomation;
        this.cancellationService = cancellationService;
        this.framework = framework;

        this.gameGui.HoveredItemChanged += this.OnHoveredItemChanged;
        this.contextMenu.OnMenuOpened += this.OnMenuOpened;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnHoveredItemChanged(object? sender, ulong e) {
        if (e != 0) this.cachedHoverItemId = e;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (this.hybridAutomation.IsActive || this.cancellationService.IsActive) return;

        bool isActiveListings = args.AddonName == "RetainerSellList";
        bool isInventory = args.AddonName != null && args.AddonName.StartsWith("Inventory");

        if ((isActiveListings || isInventory) && this.IsModifierPressed()) {
            uint resolvedItemId = 0;

            if (args.Target is MenuTargetInventory inventory && inventory.TargetItem != null) {
                resolvedItemId = inventory.TargetItem.Value.ItemId;
            }

            if (resolvedItemId == 0 || resolvedItemId >= 1000000) {
                ulong hovered = this.gameGui.HoveredItem != 0 ? this.gameGui.HoveredItem : this.cachedHoverItemId;
                if (hovered > 0 && hovered < 2000000) resolvedItemId = (uint)(hovered > 1000000 ? hovered - 1000000 : hovered);
            }

            if (resolvedItemId == 0) {
                this.logger.Warning("[ClickInterceptor] Could not resolve Item ID from native ContextMenu payload or memory Hover cache.");
                return;
            }

            if (isInventory) {
                string name = this.itemResolver.ResolveItemName(resolvedItemId) ?? $"Item #{resolvedItemId}";
                this.pendingListing = new TrackedListing { ItemId = resolvedItemId, ItemName = name };
                this.isInventorySale = true;
                this.wantsToAutomateContextMenu = true;
                this.clickTime = DateTime.Now;
                this.logger.Info($"[ClickInterceptor] Intercepted inventory item {name} (ID: {resolvedItemId}). Awaiting ContextMenu for sale.");
            }
            else {
                var activeListings = this.listingProvider.GetActiveRetainerListings();
                var currentListing = activeListings.FirstOrDefault(l => l.ItemId == resolvedItemId);

                if (currentListing == null) {
                    this.logger.Warning($"[ClickInterceptor] Item ID {resolvedItemId} not found in active listings.");
                    return;
                }

                this.pendingListing = currentListing;
                this.isInventorySale = false;
                this.wantsToAutomateContextMenu = true;
                this.clickTime = DateTime.Now;
                this.logger.Info($"[ClickInterceptor] Intercepted {currentListing.ItemName} (ID: {currentListing.ItemId}) via OnMenuOpened. Awaiting UI readiness.");
            }
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        this.EvaluateTick();
    }

    public void EvaluateTick() {
        bool isRetainerSellVisible = this.uiInteraction.IsAddonReady("RetainerSell");

        if (this.hybridAutomation.IsActive || this.cancellationService.IsActive) {
            this.wasRetainerSellVisible = isRetainerSellVisible;
            return;
        }

        // Support for Drag & Drop automation: FFXIV skips ContextMenu and opens RetainerSell directly
        if (isRetainerSellVisible && !this.wasRetainerSellVisible) {
            if (this.IsModifierPressed()) {
                this.logger.Info("[ClickInterceptor] Detected direct RetainerSell window opening (Drag & Drop) with modifier pressed. Awaiting UI text binding...");
                this.isAwaitingRetainerSellData = true;
                this.retainerSellOpenTime = DateTime.Now;
            }
        }
        this.wasRetainerSellVisible = isRetainerSellVisible;

        // Loop to wait for Atk engine to inject localized text payloads into the visible UI nodes
        if (this.isAwaitingRetainerSellData) {
            if ((DateTime.Now - this.retainerSellOpenTime).TotalSeconds > 1.5) {
                this.logger.Warning("[ClickInterceptor] Could not resolve Item ID from RetainerSell text nodes (Data binding timeout).");
                this.isAwaitingRetainerSellData = false;
            }
            else if (this.uiInteraction.GetActiveRetainerSellItemData(out var texts, out var currentPrice)) {
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

                if (resolvedItemId > 0) {
                    this.isAwaitingRetainerSellData = false;
                    this.wantsToAutomateContextMenu = false;
                    this.pendingListing = null;

                    this.logger.Info($"[ClickInterceptor] Successfully resolved dragged item {resolvedName} (ID: {resolvedItemId}) from UI text. Starting new sale automation.");
                    this.hybridAutomation.StartNewSale(resolvedItemId, resolvedName);
                }
            }
        }

        if (this.wantsToAutomateContextMenu && this.pendingListing != null) {
            if ((DateTime.Now - this.clickTime).TotalSeconds > 1.5) {
                this.logger.Warning("[ClickInterceptor] Extraction sequence timed out waiting for ContextMenu visual UI.");
                this.wantsToAutomateContextMenu = false;
                this.pendingListing = null;
                return;
            }

            if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                this.wantsToAutomateContextMenu = false;
                var listing = this.pendingListing;
                this.pendingListing = null;

                if (this.isInventorySale) {
                    var sellKeywords = new[] {
                        this.localization.Translate("InventoryMenu_SellOnMarket"),
                        this.localization.Translate("InventoryMenu_Sell")
                    }.Where(k => !string.IsNullOrWhiteSpace(k)).Cast<string>().ToList();

                    var index = this.uiInteraction.GetContextMenuItemIndex(sellKeywords);

                    if (index != -1) {
                        this.logger.Info($"[ClickInterceptor] ContextMenu ready. Auto-selling inventory item {listing.ItemName}.");
                        this.uiInteraction.SelectContextMenuItem(index);
                        this.hybridAutomation.StartNewSale(listing.ItemId, listing.ItemName);
                    }
                    else this.logger.Warning($"[ClickInterceptor] Could not find the sell option in ContextMenu for {listing.ItemName}. Please check localization files.");
                }
                else {
                    this.logger.Info($"[ClickInterceptor] ContextMenu UI ready. Delegating {listing.ItemName} to resolver.");
                    this.actionResolver.ProcessListingClick(listing);
                }
            }
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
        this.gameGui.HoveredItemChanged -= this.OnHoveredItemChanged;
        this.contextMenu.OnMenuOpened -= this.OnMenuOpened;
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}