using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.SalesScanner.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.SalesScanner.Services;

public class SalesScannerService : ISalesScannerService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ISalesHistoryScraper scraper;
    private ISalesRepository repository;
    private ILoggerService logger;

    private bool isEnabled = false;
    private bool isScanPending = false;
    private DateTime scanRequestTime;
    private int scanAttempts = 0;
    private const int MaxScanAttempts = 10;

    private static readonly IEnumerable<string> TargetAddons = new[] { "RetainerHistory" };

    public SalesScannerService(
        IAddonLifecycle addonLifecycle,
        IFramework framework,
        ISalesHistoryScraper scraper,
        ISalesRepository repository,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.scraper = scraper;
        this.repository = repository;
        this.logger = logger;
    }

    public void Enable() {
        if (this.isEnabled) {
            return;
        }

        this.addonLifecycle.RegisterListener(AddonEvent.PostSetup, TargetAddons, this.OnAddonSetup);
        this.framework.Update += this.OnFrameworkUpdate;

        this.isEnabled = true;
        this.logger.Debug("SalesScannerService enabled and listening to RetainerHistory.");
    }

    public void Disable() {
        if (!this.isEnabled) {
            return;
        }

        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, TargetAddons, this.OnAddonSetup);
        this.framework.Update -= this.OnFrameworkUpdate;

        this.isEnabled = false;
        this.logger.Debug("SalesScannerService disabled.");
    }

    private void OnAddonSetup(AddonEvent type, AddonArgs args) {
        this.logger.Info("Retainer sales history UI opened. Queuing delayed scan...");
        this.isScanPending = true;
        this.scanRequestTime = DateTime.Now;
        this.scanAttempts = 0;
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.isScanPending) {
            return;
        }

        // Wait 500ms before attempting to scan to allow the game to fetch data from the server
        if ((DateTime.Now - this.scanRequestTime).TotalMilliseconds < 500) {
            return;
        }

        this.scanRequestTime = DateTime.Now;
        this.scanAttempts++;

        var sales = this.scraper.ScrapeSales();

        if (sales.Count > 0) {
            this.repository.AddSales(sales);
            this.logger.Info($"Successfully scanned and saved {sales.Count} sales after {this.scanAttempts} attempt(s).");
            this.isScanPending = false;
        }
        else if (this.scanAttempts >= MaxScanAttempts) {
            this.logger.Warning("Failed to scan sales history after maximum attempts. The UI might be empty or still loading.");
            this.isScanPending = false;
        }
    }

    public void Dispose() {
        this.Disable();
    }
}