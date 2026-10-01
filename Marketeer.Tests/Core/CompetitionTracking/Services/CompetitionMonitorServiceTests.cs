using Dalamud.Plugin.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.CompetitionTracking.Models;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionMonitorServiceTests {
    [Fact]
    public async Task CheckUndercutsAsync_WhenListingsProcessed_CallsUpdateUndercutsOnMutator() {
        // Arrange
        var retainerState = Substitute.For<IRetainerStateService>();
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var stateMutator = Substitute.For<ICompetitionStateMutator>();
        var evaluator = Substitute.For<ICompetitionEvaluatorService>(); // Newly extracted service
        var itemResolver = Substitute.For<IItemResolverService>();
        var marketListingTracker = Substitute.For<IMarketListingTrackerService>();
        var priceCalculation = Substitute.For<IPriceCalculationService>();
        var chatGui = Substitute.For<IChatGui>();
        var localization = Substitute.For<ILocalizationService>();
        var logger = Substitute.For<ILoggerService>();

        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration());

        var clientState = Substitute.For<IClientState>();
        var framework = Substitute.For<IFramework>();

        var monitorService = new CompetitionMonitorService(
            retainerState,
            priceCache,
            stateMutator,
            evaluator, // Injected here
            itemResolver,
            marketListingTracker,
            priceCalculation,
            chatGui,
            localization,
            logger,
            configService,
            clientState,
            framework
        );

        // Act
        await monitorService.CheckUndercutsAsync();

        // Assert
        stateMutator.Received(1).UpdateUndercuts(Arg.Any<IEnumerable<UndercutItem>>());
    }
}