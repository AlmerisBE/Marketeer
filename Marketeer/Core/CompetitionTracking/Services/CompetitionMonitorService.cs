using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionMonitorService : ICompetitionMonitorService {
    private readonly IRetainerStateService retainerState;
    private readonly IServerPriceProvider priceProvider;
    private readonly ICompetitionStateService competitionState;
    private readonly IItemResolverService itemResolver;
    private readonly IMarketListingTrackerService marketListingTracker;
    private readonly IChatGui chatGui;
    private readonly ILocalizationService localization;
    private readonly ILoggerService logger;
    private readonly IConfigurationService configService;

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
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnListingsUpdated(IEnumerable<RetainerListing> newListings) {
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private void OnLocalListingModified(uint itemId) {
        Task.Run(async () => {
            var allCharacters = this.retainerState.GetAllCharactersListings();
            var worldId = allCharacters.FirstOrDefault(c => c.Listings.Any(l => l.ItemId == itemId))?.HomeWorldId ?? 0;
            if (worldId > 0) {
                await this.priceProvider.ForceRefreshAsync(new[] { itemId }, worldId);
            }
        });
    }

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
                var lowestPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, character.HomeWorldId, bypassCache: false);

                foreach (var listing in character.Listings) {
                    uint baseItemId = listing.ItemId > 1000000u ? listing.ItemId - 1000000u : listing.ItemId;
                    uint vendorPrice = config.EnforceVendorPriceMinimum ? this.itemResolver.ResolveVendorPrice(baseItemId) : 0;

                    var itemPrices = lowestPrices.Where(price => price.ItemId == listing.ItemId);

                    // Ignore market competitors strictly below the vendor sell price
                    if (config.EnforceVendorPriceMinimum && vendorPrice > 0) {
                        itemPrices = itemPrices.Where(p => p.Price >= vendorPrice);
                    }

                    var marketLowest = itemPrices.OrderBy(p => p.Price).FirstOrDefault();

                    if (marketLowest != null && marketLowest.RetainerName != listing.RetainerName) {
                        bool isWhitelisted = whitelist.Contains(marketLowest.RetainerName, StringComparer.InvariantCultureIgnoreCase) ||
                                             (autoWhitelistOwn && ownRetainers.Contains(marketLowest.RetainerName));

                        uint targetPrice;

                        if (isWhitelisted) {
                            if (config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore) {
                                continue;
                            }

                            targetPrice = marketLowest.Price;
                        }
                        else {
                            targetPrice = Math.Max(1u, marketLowest.Price - 1);
                        }

                        // Ensure our automated undercut target never dips below the vendor price
                        if (config.EnforceVendorPriceMinimum && vendorPrice > 0) {
                            targetPrice = Math.Max(vendorPrice, targetPrice);
                        }

                        if (listing.CurrentPrice <= targetPrice) {
                            continue;
                        }

                        var resolvedItemName = this.itemResolver.ResolveItemName(listing.ItemId) ?? "Unknown Item";

                        undercuts.Add(new UndercutItem {
                            SlotIndex = listing.SlotIndex,
                            ItemId = listing.ItemId,
                            ItemName = resolvedItemName,
                            Quantity = listing.Quantity,
                            RetainerName = listing.RetainerName,
                            Price = listing.CurrentPrice, // Maintained bugfix constraint from UI highlighting
                            OurPrice = listing.CurrentPrice,
                            ServerCheapestPrice = marketLowest.Price,
                            TargetPrice = targetPrice,
                            CompetitorName = marketLowest.RetainerName,
                            CharacterName = character.CharacterName
                        });
                    }
                }
            }

            this.competitionState.UpdateUndercuts(undercuts);

            if (undercuts.Any() && config.EnableChatNotifications) {
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