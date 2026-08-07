using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.Universalis.Contracts;
using Marketeer.Core.MarketWatch.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchPollerTests {
    private IChatGui chatGui;
    private IMarketWatchAnalysisService analysisService;
    private IMarketWatchAlertState alertState;
    private IMarketWatchRepository repository;
    private IServerPriceProvider priceProvider;
    private IClientState clientState;
    private ILoggerService logger;
    private IFramework framework;
    private IConfigurationService configService;

    public MarketWatchPollerTests() {
        this.chatGui = Substitute.For<IChatGui>();
        this.analysisService = Substitute.For<IMarketWatchAnalysisService>();
        this.alertState = Substitute.For<IMarketWatchAlertState>();
        this.repository = Substitute.For<IMarketWatchRepository>();
        this.priceProvider = Substitute.For<IServerPriceProvider>();
        this.clientState = Substitute.For<IClientState>();
        this.logger = Substitute.For<ILoggerService>();
        this.framework = Substitute.For<IFramework>();
        this.configService = Substitute.For<IConfigurationService>();

        // Simulates immediate framework thread execution for initialization testing
        this.framework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(x => x.Arg<Action>()());
    }

    [Fact]
    public void OnLogin_ShouldTriggerAnalysis_WhenLoggedIn() {
        this.clientState.IsLoggedIn.Returns(true);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService);

        // Manually triggers the login event
        this.clientState.Login += Raise.Event<Action>();

        // 1 call via initialization (IsLoggedIn = true) + 1 call via event = 2
        this.analysisService.Received(2).AnalyzeMarketAsync(false);
    }

    [Fact]
    public void OnPricesUpdated_ShouldAnalyzeAndPrintToChat_WhenWatchedItemUpdated() {
        this.clientState.IsLoggedIn.Returns(false);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService);

        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> {
            new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000 }
        });

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

        // Triggers price update for watched item (123) on world 1
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 123u });

        this.analysisService.Received(1).AnalyzeMarketAsync(false);
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
        this.alertState.Received(1).UpdateAlerts(alerts);
    }

    [Fact]
    public void OnPricesUpdated_ShouldNotAnalyze_WhenUnrelatedItemUpdated() {
        this.clientState.IsLoggedIn.Returns(false);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService);

        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> {
            new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000 }
        });

        // Triggers price update for an UNWATCHED item (456)
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 456u });

        this.analysisService.DidNotReceive().AnalyzeMarketAsync(Arg.Any<bool>());
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

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService);

        // Act
        // Simulating the event trigger which fires the private Task
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new[] { 1u });

        // Await minimal delay for the Task to synchronously process our mock
        await Task.Delay(50);

        // Assert (CS8625 resolved by using Arg.Any<SeString>())
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

        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework, this.configService);

        // Act
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new[] { 1u });
        await Task.Delay(50);

        // Assert (CS8625 resolved by using Arg.Any<SeString>())
        this.chatGui.DidNotReceive().Print(Arg.Any<SeString>());
    }
}