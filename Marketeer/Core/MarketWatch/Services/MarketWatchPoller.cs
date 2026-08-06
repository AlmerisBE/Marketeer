using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.Universalis.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketWatch.Services;

public class MarketWatchPoller : IDisposable {
    private IChatGui chatGui;
    private IMarketWatchAnalysisService analysisService;
    private IMarketWatchAlertState alertState;
    private IMarketWatchRepository repository;
    private IServerPriceProvider priceProvider;
    private IClientState clientState;
    private ILoggerService logger;
    private IFramework framework;

    private bool isProcessing;

    public MarketWatchPoller(
        IChatGui chatGui,
        IMarketWatchAnalysisService analysisService,
        IMarketWatchAlertState alertState,
        IMarketWatchRepository repository,
        IServerPriceProvider priceProvider,
        IClientState clientState,
        ILoggerService logger,
        IFramework framework) {

        this.chatGui = chatGui;
        this.analysisService = analysisService;
        this.alertState = alertState;
        this.repository = repository;
        this.priceProvider = priceProvider;
        this.clientState = clientState;
        this.logger = logger;
        this.framework = framework;

        // Abonnement aux notifications du cache central
        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.clientState.Login += this.OnLogin;

        // Initialisation de la graine du cache au lancement si le joueur est déjà en jeu
        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
            }
        });
    }

    public void Dispose() {
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.clientState.Login -= this.OnLogin;
    }

    private void OnLogin() {
        _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        if (this.isProcessing) {
            return;
        }

        // On vérifie que la notification concerne bien un des objets surveillés
        var watchedItemIds = this.repository.GetAllWatchedItems().Select(w => w.ItemId);
        if (watchedItemIds.Any(id => updatedItemIds.Contains(id))) {
            _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
        }
    }

    private async Task ProcessMarketAnalysisAsync(bool bypassCache) {
        this.isProcessing = true;

        try {
            var alerts = await this.analysisService.AnalyzeMarketAsync(bypassCache);
            this.alertState.UpdateAlerts(alerts);

            foreach (var alert in alerts) {
                this.NotifyAlert(alert);
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to process market watch analysis on cache update.");
        }
        finally {
            this.isProcessing = false;
        }
    }

    private void NotifyAlert(MarketWatchAlert alert) {
        var hqSymbol = alert.IsHighQuality ? " \uE03C" : "";
        var actionText = alert.AlertType == MarketWatchAlertType.BuyTargetReached ? "Buy target reached" : "Sell target reached";
        ushort colorPayload = alert.AlertType == MarketWatchAlertType.BuyTargetReached ? (ushort)45 : (ushort)43;

        var message = new SeStringBuilder()
            .AddUiForeground(548)
            .AddText("[Marketeer] ")
            .AddUiForegroundOff()
            .AddUiForeground(colorPayload)
            .AddText($"{actionText}: ")
            .AddUiForegroundOff()
            .AddText($"{alert.ItemName}{hqSymbol} ")
            .AddUiForeground(500)
            .AddText($"({alert.CurrentPrice}g) ")
            .AddUiForegroundOff()
            .AddText($"by {alert.RetainerName}.")
            .Build();

        this.chatGui.Print(message);
    }
}