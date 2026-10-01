using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.UI.Localization.Contracts;
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
    private ILocalizationService localization;

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
        IConfigurationService configService,
        ILocalizationService localization) {

        this.chatGui = chatGui;
        this.analysisService = analysisService;
        this.alertState = alertState;
        this.repository = repository;
        this.priceProvider = priceProvider;
        this.clientState = clientState;
        this.logger = logger;
        this.framework = framework;
        this.configService = configService;
        this.localization = localization;

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

    private void OnLogin() {
        _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
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

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        if (this.isProcessing) return;

        var watchedItemIds = this.repository.GetAllWatchedItems().Select(w => w.ItemId);
        if (watchedItemIds.Any(id => updatedItemIds.Contains(id))) {
            _ = this.ProcessMarketAnalysisAsync(bypassCache: false);
        }
    }

    public async Task ProcessMarketAnalysisAsync(bool bypassCache = false) {
        this.isProcessing = true;

        try {
            var oldAlerts = this.alertState.LatestAlerts;
            var newAlerts = await this.analysisService.AnalyzeMarketAsync(bypassCache);

            // Cross-reference existing alerts to extract strictly new anomalies
            var oldAlertKeys = oldAlerts.Select(a => $"{a.ItemId}_{a.IsHighQuality}").ToHashSet();
            var freshlyTriggeredAlerts = newAlerts.Where(a => !oldAlertKeys.Contains($"{a.ItemId}_{a.IsHighQuality}")).ToList();

            this.alertState.UpdateAlerts(newAlerts);

            var config = this.configService.GetConfig();
            if (config.EnableChatNotifications && freshlyTriggeredAlerts.Count > 0) {

                var summaryMsg = string.Format(
                    this.localization.Translate("MarketWatch_Notification_Summary") ?? "{0} active market opportunity(ies) ({1} new)!",
                    newAlerts.Count,
                    freshlyTriggeredAlerts.Count
                );

                this.chatGui.Print($"[Marketeer] {summaryMsg}");

                foreach (var alert in freshlyTriggeredAlerts) {
                    this.NotifyAlert(alert);
                }
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
        var watchedItem = this.repository.GetAllWatchedItems().FirstOrDefault(i => i.ItemId == alert.ItemId && i.IsHighQuality == alert.IsHighQuality);
        if (watchedItem != null && !watchedItem.EnableNotifications) return;

        var hqSymbol = alert.IsHighQuality ? " \uE03C" : "";

        string actionKey = alert.AlertType == MarketWatchAlertType.BuyTargetReached ? "MarketWatch_Action_Buy" : "MarketWatch_Action_Sell";
        string defaultAction = alert.AlertType == MarketWatchAlertType.BuyTargetReached ? "Buy target reached" : "Sell target reached";
        string localizedAction = this.localization.Translate(actionKey) ?? defaultAction;

        string priceStr = string.Format(this.localization.Translate("MarketWatch_Action_Price") ?? "({0:N0}g)", alert.CurrentPrice);
        string byStr = string.Format(this.localization.Translate("MarketWatch_Action_By") ?? "by {0}.", alert.RetainerName);

        ushort colorPayload = alert.AlertType == MarketWatchAlertType.BuyTargetReached ? (ushort)45 : (ushort)43;

        var message = new SeStringBuilder()
            .AddUiForeground(548)
            .AddText("[Marketeer] ")
            .AddUiForegroundOff()
            .AddUiForeground(colorPayload)
            .AddText($"{localizedAction}: ")
            .AddUiForegroundOff()
            .AddText($"{alert.ItemName}{hqSymbol} ")
            .AddUiForeground(500)
            .AddText($"{priceStr} ")
            .AddUiForegroundOff()
            .AddText(byStr)
            .Build();

        this.chatGui.Print(message);
    }
}