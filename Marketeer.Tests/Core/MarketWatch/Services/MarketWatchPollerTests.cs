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

    public MarketWatchPollerTests() {
        this.framework = Substitute.For<IFramework>();
        this.chatGui = Substitute.For<IChatGui>();
        this.analysisService = Substitute.For<IMarketWatchAnalysisService>();
        this.configService = Substitute.For<IConfigurationService>();
        this.logger = Substitute.For<ILoggerService>();
        this.alertState = Substitute.For<IMarketWatchAlertState>();

        var config = new PluginConfiguration { MarketWatchPollingIntervalMinutes = 30 };
        this.configService.GetConfig().Returns(config);
    }

    [Fact]
    public void OnFrameworkUpdate_ShouldNotAnalyze_WhenIntervalHasNotPassed() {
        var poller = new MarketWatchPoller(this.framework, this.chatGui, this.analysisService, this.configService, this.alertState, this.logger);

        // Simuler un appel immédiat
        poller.TriggerUpdate();

        this.analysisService.DidNotReceive().AnalyzeMarketAsync();
    }

    [Fact]
    public void TriggerUpdate_ShouldAnalyzeAndPrintToChat_WhenAlertsAreFound() {
        var poller = new MarketWatchPoller(this.framework, this.chatGui, this.analysisService, this.configService, this.alertState, this.logger);

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

        this.analysisService.AnalyzeMarketAsync().Returns(Task.FromResult((IReadOnlyList<MarketWatchAlert>)alerts));

        // Forcer le temps pour simuler l'expiration du délai
        poller.ForceLastRunTime(DateTime.MinValue);
        poller.TriggerUpdate();

        this.analysisService.Received(1).AnalyzeMarketAsync();
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
    }
}