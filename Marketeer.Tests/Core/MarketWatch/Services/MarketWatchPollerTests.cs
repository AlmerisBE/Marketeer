using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Configuration.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.Core.MarketWatch.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.MarketWatch.Services;

public class MarketWatchPollerTests {
    private IFramework framework;
    private IChatGui chatGui;
    private IMarketWatchAnalysisService analysisService;
    private IConfigurationService configService;
    private ILoggerService logger;
    private IMarketWatchAlertState alertState;
    private IObjectTable objectTable;

    public MarketWatchPollerTests() {
        this.framework = Substitute.For<IFramework>();
        this.chatGui = Substitute.For<IChatGui>();
        this.analysisService = Substitute.For<IMarketWatchAnalysisService>();
        this.configService = Substitute.For<IConfigurationService>();
        this.logger = Substitute.For<ILoggerService>();
        this.alertState = Substitute.For<IMarketWatchAlertState>();
        this.objectTable = Substitute.For<IObjectTable>();

        var config = new PluginConfiguration { MarketWatchPollingIntervalMinutes = 30 };
        this.configService.GetConfig().Returns(config);

        // Simulate a logged-in player to satisfy the first-run condition
        var player = Substitute.For<IPlayerCharacter>();
        this.objectTable.Length.Returns(1);
        this.objectTable[0].Returns(player);
    }

    [Fact]
    public void OnFrameworkUpdate_ShouldNotAnalyze_WhenIntervalHasNotPassed() {
        var poller = new MarketWatchPoller(this.framework, this.chatGui, this.analysisService, this.configService, this.alertState, this.logger, this.objectTable);

        // Consume the first async call triggered by the initial login
        poller.TriggerUpdate();
        this.analysisService.ClearReceivedCalls();
        this.chatGui.ClearReceivedCalls();

        // Simulate a new framework update before the interval expires
        poller.TriggerUpdate();

        // Verify that no analysis was triggered
        this.analysisService.DidNotReceive().AnalyzeMarketAsync(Arg.Any<bool>());
    }

    [Fact]
    public void TriggerUpdate_ShouldAnalyzeAndPrintToChat_WhenAlertsAreFound() {
        var poller = new MarketWatchPoller(this.framework, this.chatGui, this.analysisService, this.configService, this.alertState, this.logger, this.objectTable);

        var alerts = new List<MarketWatchAlert> {
            new MarketWatchAlert {
                ItemId = 123,
                ItemName = "Test Item",
                IsHighQuality = true,
                AlertType = MarketWatchAlertType.BuyTargetReached,
                CurrentPrice = 500,
                RetainerName = "Retainer A"
            }
        };

        // Configure the mock to accept the 'true' bypassCache value
        this.analysisService.AnalyzeMarketAsync(true).Returns(Task.FromResult((IReadOnlyList<MarketWatchAlert>)alerts));

        // Consume the initial login call
        poller.TriggerUpdate();
        this.analysisService.ClearReceivedCalls();
        this.chatGui.ClearReceivedCalls(); // Fix: Clear the chatGui calls as well to isolate the test

        // Force the time to simulate interval expiration
        poller.ForceLastRunTime(DateTime.MinValue);
        poller.TriggerUpdate();

        // Verify the method was called with bypassCache = true and printed once
        this.analysisService.Received(1).AnalyzeMarketAsync(true);
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
    }
}