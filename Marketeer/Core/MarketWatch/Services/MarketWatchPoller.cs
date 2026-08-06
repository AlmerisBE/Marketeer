using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using System;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketWatch.Services;

public class MarketWatchPoller : IDisposable {
    private IFramework framework;
    private IChatGui chatGui;
    private IMarketWatchAnalysisService analysisService;
    private IConfigurationService configService;
    private ILoggerService logger;

    private DateTime lastRunTime;
    private bool isProcessing;

    public MarketWatchPoller(
        IFramework framework,
        IChatGui chatGui,
        IMarketWatchAnalysisService analysisService,
        IConfigurationService configService,
        ILoggerService logger) {

        this.framework = framework;
        this.chatGui = chatGui;
        this.analysisService = analysisService;
        this.configService = configService;
        this.logger = logger;

        this.lastRunTime = DateTime.Now; // Initialize to now to prevent immediate trigger on startup
        this.framework.Update += OnFrameworkUpdate;
    }

    public void Dispose() {
        this.framework.Update -= OnFrameworkUpdate;
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (this.isProcessing) {
            return;
        }

        var intervalMinutes = this.configService.GetConfig().MarketWatchPollingIntervalMinutes;
        if (intervalMinutes <= 0) {
            return; // Feature disabled if set to 0 or negative
        }

        if ((DateTime.Now - this.lastRunTime).TotalMinutes >= intervalMinutes) {
            _ = ProcessMarketAnalysisAsync();
        }
    }

    private async Task ProcessMarketAnalysisAsync() {
        this.isProcessing = true;
        this.lastRunTime = DateTime.Now;

        try {
            var alerts = await this.analysisService.AnalyzeMarketAsync();

            foreach (var alert in alerts) {
                this.NotifyAlert(alert);
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to process market watch analysis during framework update.");
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

    // Methods required for TDD to bypass the internal timer and framework payload
    public void TriggerUpdate() => this.OnFrameworkUpdate(this.framework);
    public void ForceLastRunTime(DateTime time) => this.lastRunTime = time;
}