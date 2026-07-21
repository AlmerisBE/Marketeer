using Dalamud.Plugin.Services;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Features.UndercutTracking.Services;

public class UndercutMonitorService : IDisposable {
    private IRetainerStateService retainerState;
    private IServerPriceProvider priceProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private IClientState clientState;
    private IObjectTable objectTable;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;
    private IFramework framework;

    public UndercutMonitorService(
        IRetainerStateService retainerState,
        IServerPriceProvider priceProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        IClientState clientState,
        IObjectTable objectTable,
        IChatGui chatGui,
        ILocalizationService localization,
        ILoggerService logger,
        IFramework framework) {
        this.retainerState = retainerState;
        this.priceProvider = priceProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.chatGui = chatGui;
        this.localization = localization;
        this.logger = logger;
        this.framework = framework;

        this.retainerState.ListingsUpdated += this.OnListingsUpdated;
    }

    private void OnListingsUpdated(IEnumerable<RetainerListing> newListings) {
        this.logger.Debug("Listings updated. Triggering undercut monitor check.");

        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    public async Task CheckUndercutsAsync() {
        bool isLoggedIn = false;
        uint currentWorldId = 0;

        await this.framework.RunOnFrameworkThread(() => {
            isLoggedIn = this.clientState.IsLoggedIn;
            var player = this.objectTable.LocalPlayer;

            if (player != null && player.CurrentWorld.RowId > 0) {
                currentWorldId = player.CurrentWorld.RowId;
            }
        });

        if (!isLoggedIn || currentWorldId == 0) {
            this.logger.Debug("Cannot check undercuts: Player is not logged in or World ID is 0.");
            return;
        }

        var currentListings = this.retainerState.GetCurrentListings();
        if (!currentListings.Any()) {
            this.competitionState.UpdateUndercuts(Enumerable.Empty<UndercutItem>());
            return;
        }

        var itemIds = currentListings.Select(listing => listing.ItemId).Distinct();
        var lowestPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, currentWorldId);

        var undercuts = new List<UndercutItem>();

        foreach (var listing in currentListings) {
            var marketLowest = lowestPrices.FirstOrDefault(price => price.ItemId == listing.ItemId);

            if (marketLowest != null && marketLowest.Price < listing.CurrentPrice && marketLowest.RetainerName != listing.RetainerName) {

                var resolvedItemName = this.itemResolver.ResolveItemName(listing.ItemId) ?? "Unknown Item";

                undercuts.Add(new UndercutItem {
                    ItemId = listing.ItemId,
                    ItemName = resolvedItemName,
                    RetainerName = listing.RetainerName,
                    OurPrice = listing.CurrentPrice,
                    ServerCheapestPrice = marketLowest.Price,
                    CompetitorName = marketLowest.RetainerName
                });
            }
        }

        this.competitionState.UpdateUndercuts(undercuts);

        if (undercuts.Any()) {
            var notificationMessage = this.localization.Translate("Undercuts_Notification", undercuts.Count);
            this.chatGui.Print(notificationMessage);
        }
    }

    public void Dispose() {
        this.retainerState.ListingsUpdated -= this.OnListingsUpdated;
    }
}