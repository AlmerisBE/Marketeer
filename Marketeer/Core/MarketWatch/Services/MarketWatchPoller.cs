using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
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
    private IMarketPriceCacheService priceProvider;
    private IClientState clientState;
    private ILoggerService logger;
    private IFramework framework;
    private IConfigurationService configService;

    private bool isProcessing;
    private DateTime lastBackgroundCheck = DateTime.UtcNow;

    public MarketWatchPoller(
        IChatGui chatGui,
        IMarketWatchAnalysisService analysisService,
        IMarketWatchAlertState alertState,
        IMarketWatchRepository repository,
        IMarketPriceCacheService priceProvider,
        IClientState clientState,
        ILoggerService logger,
        IFramework framework,
        IConfigurationService configService) {

        this.chatGui = chatGui;
        this.analysisService = analysisService;
        this.alertState = alertState;
        this.repository = repository;
        this.priceProvider = priceProvider;
        this.clientState = clientState;
        this.logger = logger;
        this.framework = framework;
        this.configService = configService;

        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.clientState.Login += this.OnLogin;
        this.framework.Update += this.OnFrameworkUpdate;

        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                this.lastBackgroundCheck = DateTime.UtcNow;
                _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
            }
        });
    }

    public void Dispose() {
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.clientState.Login -= this.OnLogin;
        this.framework.Update -= this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (!this.clientState.IsLoggedIn) return;

        var config = this.configService.GetConfig();
        var cacheDuration = TimeSpan.FromMinutes(config.UniversalisCacheMinutes);

        if (DateTime.UtcNow - this.lastBackgroundCheck >= cacheDuration) {
            this.lastBackgroundCheck = DateTime.UtcNow;
            _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
        }
    }

    private void OnLogin() {
        _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        if (this.isProcessing) {
            return;
        }

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
        var config = this.configService.GetConfig();
        if (!config.EnableChatNotifications) {
            return;
        }

        var watchedItem = this.repository.GetAllWatchedItems().FirstOrDefault(i => i.ItemId == alert.ItemId && i.IsHighQuality == alert.IsHighQuality);
        if (watchedItem != null && !watchedItem.EnableNotifications) {
            return;
        }

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