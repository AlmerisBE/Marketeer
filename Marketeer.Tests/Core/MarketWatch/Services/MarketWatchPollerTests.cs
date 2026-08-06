using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
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

    public MarketWatchPollerTests() {
        this.chatGui = Substitute.For<IChatGui>();
        this.analysisService = Substitute.For<IMarketWatchAnalysisService>();
        this.alertState = Substitute.For<IMarketWatchAlertState>();
        this.repository = Substitute.For<IMarketWatchRepository>();
        this.priceProvider = Substitute.For<IServerPriceProvider>();
        this.clientState = Substitute.For<IClientState>();
        this.logger = Substitute.For<ILoggerService>();
        this.framework = Substitute.For<IFramework>();

        // Simule l'exécution immédiate du thread framework pour le test d'initialisation
        this.framework.When(x => x.RunOnFrameworkThread(Arg.Any<Action>())).Do(x => x.Arg<Action>()());
    }

    [Fact]
    public void OnLogin_ShouldTriggerAnalysis_WhenLoggedIn() {
        this.clientState.IsLoggedIn.Returns(true);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework);

        // Déclenche l'événement de connexion manuellement
        this.clientState.Login += Raise.Event<Action>();

        // 1 appel via l'initialisation (car IsLoggedIn = true) + 1 appel via l'événement = 2
        this.analysisService.Received(2).AnalyzeMarketAsync(false);
    }

    [Fact]
    public void OnPricesUpdated_ShouldAnalyzeAndPrintToChat_WhenWatchedItemUpdated() {
        this.clientState.IsLoggedIn.Returns(false);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework);

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

        // Déclenche la mise à jour des prix pour l'objet surveillé (123) sur le monde 1
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 123u });

        this.analysisService.Received(1).AnalyzeMarketAsync(false);
        this.chatGui.Received(1).Print(Arg.Any<SeString>());
        this.alertState.Received(1).UpdateAlerts(alerts);
    }

    [Fact]
    public void OnPricesUpdated_ShouldNotAnalyze_WhenUnrelatedItemUpdated() {
        this.clientState.IsLoggedIn.Returns(false);
        var poller = new MarketWatchPoller(this.chatGui, this.analysisService, this.alertState, this.repository, this.priceProvider, this.clientState, this.logger, this.framework);

        this.repository.GetAllWatchedItems().Returns(new List<WatchedItem> {
            new WatchedItem { ItemId = 123, IsHighQuality = false, TargetBuyPrice = 1000 }
        });

        // Déclenche la mise à jour pour un objet NON surveillé (456)
        this.priceProvider.PricesUpdated += Raise.Event<Action<uint, IEnumerable<uint>>>(1u, new List<uint> { 456u });

        this.analysisService.DidNotReceive().AnalyzeMarketAsync(Arg.Any<bool>());
    }
}