using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionMonitorServiceTests {
    [Fact]
    public async Task CheckUndercutsAsync_WhenUndercutDetected_ShouldUpdateStateAndNotify() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalculationService = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { EnableChatNotifications = true };
        configService.GetConfig().Returns(config);

        var characterListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "TestPlayer",
                HomeWorldId = 99,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 1, CurrentPrice = 1000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };

        retainerState.GetAllCharactersListings().Returns(characterListings);

        var marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1, Price = 900, RetainerName = "Competitor" }
        };
        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 99, false).Returns(marketPrices);

        var calcResult = new PriceCalculationResult { Action = PricingAction.UpdatePrice, CalculatedPrice = 899 };
        priceCalculationService.CalculateTargetPrice(1, 1000, Arg.Any<IReadOnlyList<LowestPriceResult>>()).Returns(calcResult);

        using var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            priceCalculationService, chatGui, localization, logger, configService, clientState, framework);

        await service.CheckUndercutsAsync();

        competitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(items => items.Any(i => i.TargetPrice == 899)));
        chatGui.Received(1).Print(Arg.Any<string>());
    }

    [Fact]
    public async Task CheckUndercutsAsync_WhenNoUndercut_ShouldNotUpdateStateOrNotify() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalculationService = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { EnableChatNotifications = true };
        configService.GetConfig().Returns(config);

        var characterListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "TestPlayer",
                HomeWorldId = 99,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 1, CurrentPrice = 500, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };

        retainerState.GetAllCharactersListings().Returns(characterListings);

        var marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1, Price = 600, RetainerName = "Competitor" }
        };
        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 99, false).Returns(marketPrices);

        var calcResult = new PriceCalculationResult { Action = PricingAction.KeepPrice, CalculatedPrice = 500 };
        priceCalculationService.CalculateTargetPrice(1, 500, Arg.Any<IReadOnlyList<LowestPriceResult>>()).Returns(calcResult);

        using var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            priceCalculationService, chatGui, localization, logger, configService, clientState, framework);

        await service.CheckUndercutsAsync();

        competitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(items => !items.Any()));
        chatGui.DidNotReceiveWithAnyArgs().Print(string.Empty);
    }

    [Fact]
    public async Task CheckUndercutsAsync_WhenActionIsCancel_ShouldIncludeInUndercutStateForCancellation() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IMarketPriceCacheService>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalculationService = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var configService = Substitute.For<IConfigurationService>();
        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        var config = new PluginConfiguration { EnableChatNotifications = true };
        configService.GetConfig().Returns(config);

        var characterListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "TestPlayer",
                HomeWorldId = 99,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 1, CurrentPrice = 1000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };

        retainerState.GetAllCharactersListings().Returns(characterListings);

        var marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 1, Price = 50, RetainerName = "Competitor" }
        };
        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), 99, false).Returns(marketPrices);

        var calcResult = new PriceCalculationResult { Action = PricingAction.CancelListing, CalculatedPrice = 1000 };
        priceCalculationService.CalculateTargetPrice(1, 1000, Arg.Any<IReadOnlyList<LowestPriceResult>>()).Returns(calcResult);

        using var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            priceCalculationService, chatGui, localization, logger, configService, clientState, framework);

        await service.CheckUndercutsAsync();

        competitionState.Received(1).UpdateUndercuts(Arg.Is<IEnumerable<UndercutItem>>(items => items.Any(i => i.SuggestedAction == PricingAction.CancelListing)));
    }
}