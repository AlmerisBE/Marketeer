using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Services;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchPollerTests {
    private IChatGui chatGui;
    private IMarketWatchAnalysisService analysisService;
    private IMarketWatchAlertState alertState;
    private IMarketWatchRepository repository;
    private IMarketPriceCacheService priceProvider;
    private IClientState clientState;
    private ILoggerService logger;
    private IFramework framework;
    private IConfigurationService configService;
    private ILocalizationService localization;

    public MarketWatchPollerTests() {
        this.chatGui = Substitute.For<IChatGui>();
        this.analysisService = Substitute.For<IMarketWatchAnalysisService>();
        this.alertState = Substitute.For<IMarketWatchAlertState>();
        this.repository = Substitute.For<IMarketWatchRepository>();
        this.priceProvider = Substitute.For<IMarketPriceCacheService>();
        this.clientState = Substitute.For<IClientState>();
        this.logger = Substitute.For<ILoggerService>();
        this.framework = Substitute.For<IFramework>();
        this.configService = Substitute.For<IConfigurationService>();
        this.localization = Substitute.For<ILocalizationService>();

        // Simulates immediate framework thread execution for initialization testing
        this.framework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(x => x.Arg<Action>()());
    }

    [Fact]
    public async Task OnLogin_ShouldTriggerAnalysis_WhenLoggedIn() {
        // Arrange
        this.clientState.IsLoggedIn.Returns(true);
        var config = new PluginConfiguration { EnableChatNotifications = true };
        this.configService.GetConfig().Returns(config);

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        // Act - Manually triggers the login event
        this.clientState.Login += Raise.Event<Action>();
        await Task.Delay(50);

        // Assert - 1 call via initialization (IsLoggedIn = true) + 1 call via event = 2
        await this.analysisService.Received(2).AnalyzeMarketAsync(false);
    }

    [Fact]
    public async Task OnPricesUpdated_ShouldAnalyzeAndPrintToChat_WhenWatchedItemUpdated() {
        // Arrange
        this.clientState.IsLoggedIn.Returns(false);
        var config = new PluginConfiguration { EnableChatNotifications = true };
        this.configService.GetConfig().Returns(config);

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> {
            new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000, EnableNotifications = true }
        }.AsReadOnly());

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert {
                ItemId = 123,
                ItemName = "Test Item",
                IsHighQuality = false,
                AlertType = MarketWatchAlertType.BuyTargetReached,
                CurrentPrice = 500,
                RetainerName = "Retainer A"
            }
        };

        this.analysisService.AnalyzeMarketAsync(false).Returns(Task.FromResult((IReadOnlyList<MarketWatchAlert>)alerts));

        // Act - Triggers price update for watched item (123) on world 1
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 123u });
        await Task.Delay(50);

        // Assert
        await this.analysisService.Received(1).AnalyzeMarketAsync(false);

        // Assert that the summary notification and the specific alert are both broadcasted
        this.chatGui.Received(1).Print(Arg.Any<string>());
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
        this.alertState.Received(1).UpdateAlerts(alerts);
    }

    [Fact]
    public async Task OnPricesUpdated_ShouldNotAnalyze_WhenUnrelatedItemUpdated() {
        // Arrange
        this.clientState.IsLoggedIn.Returns(false);
        var config = new PluginConfiguration { EnableChatNotifications = true };
        this.configService.GetConfig().Returns(config);

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> {
            new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000 }
        }.AsReadOnly());

        // Act - Triggers price update for an UNWATCHED item (456)
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 456u });
        await Task.Delay(50);

        // Assert
        await this.analysisService.DidNotReceive().AnalyzeMarketAsync(Arg.Any<bool>());
    }

    [Fact]
    public async Task ProcessMarketAnalysisAsync_ShouldNotNotify_WhenGlobalNotificationsDisabled() {
        // Arrange
        var config = new PluginConfiguration { EnableChatNotifications = false };
        this.configService.GetConfig().Returns(config);

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert { ItemId = 1, ItemName = "Test Item", AlertType = MarketWatchAlertType.BuyTargetReached }
        };
        this.analysisService.AnalyzeMarketAsync(Arg.Any<bool>()).Returns(Task.FromResult((IReadOnlyList<MarketWatchAlert>)alerts));

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        // Act - Call the method synchronously instead of relying on event timing
        await poller.ProcessMarketAnalysisAsync();

        // Assert
        this.chatGui.DidNotReceive().Print(Arg.Any<string>());
        this.chatGui.DidNotReceive().Print(Arg.Any<SeString>());
    }

    [Fact]
    public async Task ProcessMarketAnalysisAsync_ShouldNotNotify_WhenItemNotificationsDisabled() {
        // Arrange
        var config = new PluginConfiguration { EnableChatNotifications = true };
        this.configService.GetConfig().Returns(config);

        var watchedItem = new WatchedItem { ItemId = 1, EnableNotifications = false };
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { watchedItem }.AsReadOnly());

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert { ItemId = 1, ItemName = "Test Item", AlertType = MarketWatchAlertType.BuyTargetReached }
        };
        this.analysisService.AnalyzeMarketAsync(Arg.Any<bool>()).Returns(Task.FromResult((IReadOnlyList<MarketWatchAlert>)alerts));

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        // Act
        await poller.ProcessMarketAnalysisAsync();

        // Assert
        this.chatGui.DidNotReceive().Print(Arg.Any<SeString>());
    }

    [Fact]
    public async Task ProcessMarketAnalysisAsync_WhenAlertPersists_DoesNotSpamChat() {
        // Arrange
        var config = new PluginConfiguration { EnableChatNotifications = true };
        this.configService.GetConfig().Returns(config);

        var watchedItem = new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000, EnableNotifications = true };
        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> { watchedItem }.AsReadOnly());

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert { ItemId = 123, IsHighQuality = false, AlertType = MarketWatchAlertType.BuyTargetReached, CurrentPrice = 1000 }
        };
        this.analysisService.AnalyzeMarketAsync(Arg.Any<bool>()).Returns(alerts);

        // Using a concrete state instance here since we test internal delta retention logic
        var concreteAlertState = new MarketWatchAlertState();

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, concreteAlertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService, this.localization);

        // Act - Process the exact same state twice sequentially
        await poller.ProcessMarketAnalysisAsync();
        await poller.ProcessMarketAnalysisAsync();

        // Assert - Only 1 summary and 1 alert should trigger, completely ignoring the second pass
        this.chatGui.Received(1).Print(Arg.Any<string>());
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
    }
}