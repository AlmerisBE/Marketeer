using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.Universalis.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionMonitorService : ICompetitionMonitorService {
    private IRetainerStateService retainerState;
    private IServerPriceProvider priceProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private IMarketListingTrackerService marketListingTracker;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IConfigurationService configService;

    private bool isMonitoring;
    private bool isChecking;

    public CompetitionMonitorService(
        IRetainerStateService retainerState,
        IServerPriceProvider priceProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        IMarketListingTrackerService marketListingTracker,
        IChatGui chatGui,
        ILocalizationService localization,
        ILoggerService logger,
        IConfigurationService configService) {

        this.retainerState = retainerState;
        this.priceProvider = priceProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.marketListingTracker = marketListingTracker;
        this.chatGui = chatGui;
        this.localization = localization;
        this.logger = logger;
        this.configService = configService;

        this.isMonitoring = false;
        this.isChecking = false;
    }

    public void StartMonitoring() {
        if (this.isMonitoring) {
            return;
        }

        this.retainerState.ListingsUpdated += this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified += this.OnLocalListingModified;

        // Abonnement à l'horloge centrale Universalis
        this.priceProvider.PricesUpdated += this.OnPricesUpdated;

        this.isMonitoring = true;
        this.logger.Info("Undercut monitor service started (Event-Driven).");
    }

    public void StopMonitoring() {
        if (!this.isMonitoring) {
            return;
        }

        this.retainerState.ListingsUpdated -= this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified -= this.OnLocalListingModified;
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;

        this.isMonitoring = false;
        this.logger.Info("Undercut monitor service stopped.");
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        // Déclenche une analyse globale basée sur les nouvelles données fraîches en cache
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnListingsUpdated(IEnumerable<RetainerListing> newListings) {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnLocalListingModified(uint itemId) {
        // En cas de modification locale, on force l'invalidation du cache Universalis.
        // Cela provoquera une requête serveur, qui lancera l'event PricesUpdated, qui lancera CheckUndercutsAsync.
        Task.Run(async () => {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var worldId = allCharacters.FirstOrDefault(c => c.Listings.Any(l => l.ItemId == itemId))?.HomeWorldId ?? 0;
            if (worldId > 0) {
                await this.priceProvider.ForceRefreshAsync(new[] { itemId }, worldId);
            }
        });
    }

    // Le code de CheckUndercutForItemAsync est supprimé, car OnLocalListingModified utilise la cascade d'événements.
    public Task CheckUndercutForItemAsync(uint itemId) => Task.CompletedTask;

    public async Task CheckUndercutsAsync() {
        if (this.isChecking) {
            return;
        }

        this.isChecking = true;

        try {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var undercuts = new List<UndercutItem>();

            var config = this.configService.GetConfig();
            var whitelist = config.CompetitorWhitelist ?? new List<string>();
            var autoWhitelistOwn = config.AutoWhitelistOwnRetainers;
            var ownRetainers = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (autoWhitelistOwn && config.FinancialRecords != null) {
                foreach (var cData in config.FinancialRecords.Values) {
                    foreach (var rData in cData.Retainers.Values) {
                        ownRetainers.Add(rData.Name);
                    }
                }
            }

            foreach (var character in allCharacters) {
                if (!character.Listings.Any()) {
                    continue;
                }

                var itemIds = character.Listings.Select(listing => listing.ItemId).Distinct();

                // bypassCache est à FALSE. On utilise les données que le timer Universalis vient de rafraîchir.
                var lowestPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, character.HomeWorldId, bypassCache: false);

                foreach (var listing in character.Listings) {
                    var marketLowest = lowestPrices.Where(price => price.ItemId == listing.ItemId).OrderBy(p => p.Price).FirstOrDefault();

                    if (marketLowest != null && marketLowest.Price < listing.CurrentPrice && marketLowest.RetainerName != listing.RetainerName) {

                        if (whitelist.Contains(marketLowest.RetainerName, StringComparer.InvariantCultureIgnoreCase)) {
                            continue;
                        }

                        if (autoWhitelistOwn && ownRetainers.Contains(marketLowest.RetainerName)) {
                            continue;
                        }

                        var resolvedItemName = this.itemResolver.ResolveItemName(listing.ItemId) ?? "Unknown Item";

                        undercuts.Add(new UndercutItem {
                            SlotIndex = listing.SlotIndex,
                            ItemId = listing.ItemId,
                            ItemName = resolvedItemName,
                            Quantity = listing.Quantity,
                            RetainerName = listing.RetainerName,
                            OurPrice = listing.CurrentPrice,
                            ServerCheapestPrice = marketLowest.Price,
                            CompetitorName = marketLowest.RetainerName,
                            CharacterName = character.CharacterName
                        });
                    }
                }
            }

            this.competitionState.UpdateUndercuts(undercuts);

            if (undercuts.Any()) {
                var notificationMessage = this.localization.Translate("Undercuts_Notification", undercuts.Count);
                this.chatGui.Print(notificationMessage);
            }
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