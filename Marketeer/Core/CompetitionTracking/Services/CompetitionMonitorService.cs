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

public class CompetitionMonitorService : ICompetitionMonitorService, IDisposable {
    private IRetainerStateService retainerState;
    private IServerPriceProvider priceProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private IMarketListingTrackerService marketListingTracker;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IFramework framework;
    private IConfigurationService configService;

    private bool isMonitoring;
    private bool isChecking;
    private DateTime lastScanTime;
    private readonly TimeSpan scanInterval = TimeSpan.FromMinutes(10);

    public CompetitionMonitorService(
        IRetainerStateService retainerState,
        IServerPriceProvider priceProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        IMarketListingTrackerService marketListingTracker,
        IChatGui chatGui,
        ILocalizationService localization,
        ILoggerService logger,
        IFramework framework,
        IConfigurationService configService) {

        this.retainerState = retainerState;
        this.priceProvider = priceProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.marketListingTracker = marketListingTracker;
        this.chatGui = chatGui;
        this.localization = localization;
        this.logger = logger;
        this.framework = framework;
        this.configService = configService;

        this.isMonitoring = false;
        this.isChecking = false;
        this.lastScanTime = DateTime.MinValue;
    }

    public void StartMonitoring() {
        if (this.isMonitoring) {
            return;
        }

        this.retainerState.ListingsUpdated += this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified += this.OnLocalListingModified;
        this.framework.Update += this.OnFrameworkUpdate;

        this.isMonitoring = true;
        this.logger.Info("Undercut monitor service started.");
    }

    public void StopMonitoring() {
        if (!this.isMonitoring) {
            return;
        }

        this.retainerState.ListingsUpdated -= this.OnListingsUpdated;
        this.marketListingTracker.LocalListingModified -= this.OnLocalListingModified;
        this.framework.Update -= this.OnFrameworkUpdate;

        this.isMonitoring = false;
        this.logger.Info("Undercut monitor service stopped.");
    }

    private void OnListingsUpdated(IEnumerable<RetainerListing> newListings) {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnLocalListingModified(uint itemId) {
        Task.Run(async () => await this.CheckUndercutForItemAsync(itemId));
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (DateTime.Now - this.lastScanTime >= this.scanInterval) {
            this.lastScanTime = DateTime.Now;
            Task.Run(async () => await this.CheckUndercutsAsync());
        }
    }

    public async Task CheckUndercutForItemAsync(uint itemId) {
        try {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var newUndercuts = new List<UndercutItem>();

            var config = this.configService.GetConfig();
            var whitelist = config.CompetitorWhitelist;
            var autoWhitelistOwn = config.AutoWhitelistOwnRetainers;
            var ownRetainers = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (autoWhitelistOwn) {
                foreach (var cData in config.FinancialRecords.Values) {
                    foreach (var rData in cData.Retainers.Values) {
                        ownRetainers.Add(rData.Name);
                    }
                }
            }

            foreach (var character in allCharacters) {
                if (!character.Listings.Any(l => l.ItemId == itemId)) {
                    continue;
                }

                var lowestPriceResult = await this.priceProvider.GetLowestPriceAsync(itemId, character.HomeWorldId);
                if (lowestPriceResult == null) {
                    continue;
                }

                var itemListings = character.Listings.Where(l => l.ItemId == itemId);
                foreach (var listing in itemListings) {
                    if (lowestPriceResult.Price < listing.CurrentPrice && lowestPriceResult.RetainerName != listing.RetainerName) {

                        if (whitelist.Contains(lowestPriceResult.RetainerName, StringComparer.InvariantCultureIgnoreCase)) {
                            continue;
                        }

                        if (autoWhitelistOwn && ownRetainers.Contains(lowestPriceResult.RetainerName)) {
                            continue;
                        }

                        var resolvedItemName = this.itemResolver.ResolveItemName(itemId) ?? "Unknown Item";

                        newUndercuts.Add(new UndercutItem {
                            SlotIndex = listing.SlotIndex,
                            ItemId = itemId,
                            ItemName = resolvedItemName,
                            Quantity = listing.Quantity,
                            RetainerName = listing.RetainerName,
                            OurPrice = listing.CurrentPrice,
                            ServerCheapestPrice = lowestPriceResult.Price,
                            CompetitorName = lowestPriceResult.RetainerName,
                            CharacterName = character.CharacterName
                        });
                    }
                }
            }

            this.competitionState.UpdateItemUndercuts(itemId, newUndercuts);
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to check undercut for item {itemId} in background task.");
        }
    }

    public async Task CheckUndercutsAsync() {
        if (this.isChecking) {
            return;
        }

        this.isChecking = true;

        try {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var undercuts = new List<UndercutItem>();

            var config = this.configService.GetConfig();
            var whitelist = config.CompetitorWhitelist;
            var autoWhitelistOwn = config.AutoWhitelistOwnRetainers;
            var ownRetainers = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (autoWhitelistOwn) {
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
                var lowestPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, character.HomeWorldId);

                foreach (var listing in character.Listings) {
                    var marketLowest = lowestPrices.FirstOrDefault(price => price.ItemId == listing.ItemId);

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