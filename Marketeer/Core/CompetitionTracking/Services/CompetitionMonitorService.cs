using Dalamud.Plugin.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionMonitorService : ICompetitionMonitorService {
    private IRetainerStateService retainerState;
    private IMarketPriceCacheService priceProvider;
    private ICompetitionStateMutator stateMutator;
    private ICompetitionEvaluatorService evaluatorService;
    private IItemResolverService itemResolver;
    private IMarketListingTrackerService marketListingTracker;
    private IPriceCalculationService priceCalculationService;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IConfigurationService configService;
    private IClientState clientState;
    private IFramework framework;

    private bool isMonitoring;
    private bool isChecking;
    private DateTime lastBackgroundCheck = DateTime.UtcNow;
    private IReadOnlyList<UndercutItem> undercuts = new List<UndercutItem>();

    // Tracks previously notified undercuts to prevent chat spam
    private HashSet<uint> knownUndercutItemIds = new();

    public CompetitionMonitorService(
        IRetainerStateService retainerState,
        IMarketPriceCacheService priceProvider,
        ICompetitionStateMutator stateMutator,
        ICompetitionEvaluatorService evaluatorService,
        IItemResolverService itemResolver,
        IMarketListingTrackerService marketListingTracker,
        IPriceCalculationService priceCalculationService,
        IChatGui chatGui,
        ILocalizationService localization,
        ILoggerService logger,
        IConfigurationService configService,
        IClientState clientState,
        IFramework framework) {

        this.retainerState = retainerState;
        this.priceProvider = priceProvider;
        this.stateMutator = stateMutator;
        this.evaluatorService = evaluatorService;
        this.itemResolver = itemResolver;
        this.marketListingTracker = marketListingTracker;
        this.priceCalculationService = priceCalculationService;
        this.chatGui = chatGui;
        this.localization = localization;
        this.logger = logger;
        this.configService = configService;
        this.clientState = clientState;
        this.framework = framework;

        this.isMonitoring = false;
        this.isChecking = false;
    }

    public void StartMonitoring() {
        if (this.isMonitoring) return;

        this.retainerState.ListingsUpdated += this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified += this.OnLocalListingModified;
        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.clientState.Login += this.OnLogin;
        this.framework.Update += this.OnFrameworkUpdate;

        this.isMonitoring = true;
        this.logger.Info("Undercut monitor service started (Event-Driven & Polling).");

        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                this.lastBackgroundCheck = DateTime.UtcNow;
                Task.Run(async () => await this.CheckUndercutsAsync());
            }
        });
    }

    public void StopMonitoring() {
        if (!this.isMonitoring) return;

        this.retainerState.ListingsUpdated -= this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified -= this.OnLocalListingModified;
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.clientState.Login -= this.OnLogin;
        this.framework.Update -= this.OnFrameworkUpdate;

        this.isMonitoring = false;
        this.logger.Info("Undercut monitor service stopped.");
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.isMonitoring || !this.clientState.IsLoggedIn) return;

        var config = this.configService.GetConfig();
        var cacheDuration = TimeSpan.FromMinutes(config.UniversalisCacheMinutes);

        if (DateTime.UtcNow - this.lastBackgroundCheck >= cacheDuration) {
            this.lastBackgroundCheck = DateTime.UtcNow;
            Task.Run(async () => await this.CheckUndercutsAsync());
        }
    }

    private void OnLogin() {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnListingsUpdated(IEnumerable<RetainerListing> newListings) {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnLocalListingModified(uint itemId) {
        this.stateMutator.UpdateItemUndercuts(itemId, Enumerable.Empty<UndercutItem>());

        Task.Run(async () => {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var worldId = allCharacters.FirstOrDefault(c => c.Listings.Any(l => l.ItemId == itemId))?.HomeWorldId ?? 0;
            if (worldId > 0) {
                uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
                await this.priceProvider.ForceRefreshAsync(new[] { baseItemId }, worldId);
            }
        });
    }

    public Task CheckUndercutForItemAsync(uint itemId) => Task.CompletedTask;

    public IReadOnlyList<UndercutItem> GetInternalUndercutItems() {
        return this.undercuts;
    }

    public async Task CheckUndercutsAsync() {
        if (this.isChecking) return;
        this.isChecking = true;

        try {
            var initialCharacters = this.retainerState.GetAllCharactersListings();
            var newUndercuts = new List<UndercutItem>();
            var config = this.configService.GetConfig();

            foreach (var character in initialCharacters) {
                if (!character.Listings.Any()) continue;

                var itemIds = character.Listings.Select(listing => listing.ItemId).Distinct();
                var pricings = await this.priceProvider.GetPricingsAsync(itemIds, character.HomeWorldId, bypassCache: false);

                var liveCharacters = this.retainerState.GetAllCharactersListings();
                var liveCharacter = liveCharacters.FirstOrDefault(c => c.CharacterName == character.CharacterName && c.HomeWorldId == character.HomeWorldId);
                if (liveCharacter == null) continue;

                foreach (var listing in liveCharacter.Listings) {
                    var itemPricing = pricings.FirstOrDefault(p => p.ItemId == listing.ItemId);
                    if (itemPricing == null) continue;

                    var resolvedItemName = this.itemResolver.ResolveItemName(listing.ItemId) ?? "Unknown Item";

                    if (this.evaluatorService.TryEvaluateListing(listing, itemPricing, character.CharacterName, resolvedItemName, out var undercutResult) && undercutResult != null) {
                        newUndercuts.Add(undercutResult);
                    }
                }
            }

            this.stateMutator.UpdateUndercuts(newUndercuts);

            // Determine if there are *new* items being undercut that we haven't warned the user about yet
            var currentUndercutIds = newUndercuts.Select(u => u.ItemId).ToHashSet();
            bool hasNewUndercuts = currentUndercutIds.Except(this.knownUndercutItemIds).Any();

            if (hasNewUndercuts && config.EnableChatNotifications) {
                var notificationMessage = this.localization.Translate("Undercuts_Notification", newUndercuts.Count) ?? $"[Marketeer] {newUndercuts.Count} items are undercut!";
                this.chatGui.Print(notificationMessage);
            }

            // Sync state so we don't alert for these same items next iteration
            this.knownUndercutItemIds = currentUndercutIds;
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to check undercuts in background task.");
        }
        finally {
            this.isChecking = false;
        }
    }

    public void Dispose() {
        this.StopMonitoring();
    }
}