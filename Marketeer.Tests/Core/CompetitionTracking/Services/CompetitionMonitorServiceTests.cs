using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionMonitorServiceTests {
    [Fact]
    public async Task CheckUndercutsAsync_ShouldUpdateStateAndNotify_WhenUndercutsFound() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false,
            EnforceVendorPriceMinimum = false
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4000, RetainerName = "CompetitorX", IsHq = false }
        };

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        // Asserts that the item is undercut and the target price is correctly set to 4000 - 1 = 3999
        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "CompetitorX" && u.TargetPrice == 3999))
        );
    }

    [Fact]
    public async Task CheckUndercutsAsync_ShouldMatchPrice_WhenCompetitorIsWhitelistedAndBehaviorIsMatch() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false,
            CompetitorWhitelist = new List<string> { "FriendlyRetainer" },
            CompetitorWhitelistBehavior = WhitelistBehavior.MatchPrice,
            EnforceVendorPriceMinimum = false
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false }
        };

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        // Asserts that the target price exactly matches the whitelisted competitor's price (4000)
        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "FriendlyRetainer" && u.TargetPrice == 4000))
        );
    }

    [Fact]
    public async Task CheckUndercutsAsync_ShouldIgnoreCompetitor_WhenCompetitorIsWhitelistedAndBehaviorIsIgnore() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false,
            CompetitorWhitelist = new List<string> { "FriendlyRetainer" },
            CompetitorWhitelistBehavior = WhitelistBehavior.Ignore,
            EnforceVendorPriceMinimum = false
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 4000, RetainerName = "FriendlyRetainer", IsHq = false }
        };

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        // Asserts that no undercut items are registered since the only competitor is being ignored
        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => !list.Any())
        );
    }

    [Fact]
    public async Task CheckUndercutsAsync_ShouldIgnoreCompetitor_WhenBelowVendorPriceAndEnforcementEnabled() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false,
            EnforceVendorPriceMinimum = true
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");
        itemResolver.ResolveVendorPrice(100).Returns(2000u);

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        // Competitor is selling for 1500, which is below the NPC vendor price of 2000
        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 1500, RetainerName = "CompetitorX", IsHq = false }
        };

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        // Asserts that no undercut items are registered because the competitor was filtered out
        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => !list.Any())
        );
    }

    [Fact]
    public async Task CheckUndercutsAsync_ShouldClampTargetPriceToVendorPrice_WhenCompetitorAboveVendorPriceAndEnforcementEnabled() {
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceProvider = Substitute.For<IServerPriceProvider>();
        var competitionState = Substitute.For<ICompetitionStateService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var configService = Substitute.For<IConfigurationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();

        var config = new PluginConfiguration {
            AutoWhitelistOwnRetainers = false,
            EnforceVendorPriceMinimum = true
        };
        configService.GetConfig().Returns(config);
        itemResolver.ResolveItemName(100).Returns("Test Item");
        itemResolver.ResolveVendorPrice(100).Returns(2000u);

        var myListings = new List<CharacterMarketData> {
            new CharacterMarketData {
                CharacterName = "MyCharacter",
                HomeWorldId = 1,
                Listings = new List<RetainerListing> {
                    new RetainerListing { ItemId = 100, CurrentPrice = 5000, RetainerName = "MyRetainer", SlotIndex = 0 }
                }
            }
        };
        retainerState.GetAllCharactersListings().Returns(myListings);

        // Competitor is selling for exactly 2000. Normal undercut would be 1999.
        IReadOnlyList<LowestPriceResult> marketPrices = new List<LowestPriceResult> {
            new LowestPriceResult { ItemId = 100, Price = 2000, RetainerName = "CompetitorX", IsHq = false }
        };

        priceProvider.GetLowestPricesAsync(Arg.Any<IEnumerable<uint>>(), Arg.Any<uint>(), false)
            .Returns(Task.FromResult(marketPrices));

        var service = new CompetitionMonitorService(
            retainerState, priceProvider, competitionState, itemResolver, marketListingTracker,
            chatGui, localization, logger, configService);

        await service.CheckUndercutsAsync();

        // Asserts that the target price is clamped to the vendor minimum (2000) instead of 1999
        competitionState.Received(1).UpdateUndercuts(
            Arg.Is<IEnumerable<UndercutItem>>(list => list.Any(u => u.CompetitorName == "CompetitorX" && u.TargetPrice == 2000))
        );
    }
}