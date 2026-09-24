using Dalamud.Plugin.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.UI.CompetitionTracking.Commands;

public class PriceCommand : ICommand {
    private IMarketPriceCacheService priceCache;
    private IItemResolverService itemResolver;
    private ICompetitionPlayerContext playerContext;
    private IChatGui chatGui;
    private ILoggerService logger;
    private IFramework framework;

    public string CommandTrigger => "price";
    public string Description => "Check the current lowest price for an item. Usage: /marketeer price <item name>";

    public PriceCommand(
        IMarketPriceCacheService priceCache,
        IItemResolverService itemResolver,
        ICompetitionPlayerContext playerContext,
        IChatGui chatGui,
        ILoggerService logger,
        IFramework framework) {

        this.priceCache = priceCache;
        this.itemResolver = itemResolver;
        this.playerContext = playerContext;
        this.chatGui = chatGui;
        this.logger = logger;
        this.framework = framework;
    }

    public void Execute(string arguments) {
        // Satisfies ICommand synchronously while adopting fire-and-forget correctly
        _ = this.ExecuteAsync(arguments);
    }

    public async Task ExecuteAsync(string arguments) {
        if (string.IsNullOrWhiteSpace(arguments)) {
            this.chatGui.Print("Usage: /marketeer price <item name>");
            return;
        }

        var itemName = arguments.Trim();
        var itemId = this.itemResolver.ResolveItemId(itemName);

        if (itemId == 0) {
            this.chatGui.PrintError($"[Marketeer] Item '{itemName}' could not be found.");
            return;
        }

        if (!this.playerContext.IsPlayerAvailable()) return;

        var worldId = this.playerContext.GetCurrentWorldId();

        try {
            var pricing = await this.priceCache.GetPricingAsync(itemId, worldId, false);

            _ = this.framework.RunOnFrameworkThread(() => {
                if (pricing == null || pricing.Listings.Count == 0) {
                    this.chatGui.Print($"[Marketeer] No active listings found for {itemName} on your current world.");
                    if (pricing != null && pricing.AverageSalePrice > 0) this.chatGui.Print($"[Marketeer] Average Universalis historical sale price: {pricing.AverageSalePrice:N0}g.");
                    return;
                }

                var lowestListing = pricing.Listings.OrderBy(l => l.Price).First();
                var hqMarker = lowestListing.IsHq ? " \uE03C" : "";

                this.chatGui.Print($"[Marketeer] Lowest price for {itemName}{hqMarker} is {lowestListing.Price:N0}g by {lowestListing.RetainerName}.");

                if (pricing.AverageSalePrice > 0) this.chatGui.Print($"[Marketeer] Average Universalis historical sale price: {pricing.AverageSalePrice:N0}g.");
            });
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to fetch price for {itemName}.");
            _ = this.framework.RunOnFrameworkThread(() => this.chatGui.PrintError($"[Marketeer] Error fetching price for {itemName}."));
        }
    }
}