using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesScannerService : ISalesScannerService, IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IFramework framework;
    private ISalesHistoryScraper scraper;
    private ISalesRepository repository;
    private ILoggerService logger;

    private bool isEnabled = false;
    private bool isScanActive = false;
    private DateTime lastScanTime;

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
        this.addonLifecycle.RegisterListener(AddonEvent.PreFinalize, TargetAddons, this.OnAddonFinalize);
        this.framework.Update += this.OnFrameworkUpdate;

        this.isEnabled = true;
        this.logger.Debug("SalesScannerService enabled and listening to RetainerHistory.");
    }

    public void Disable() {
        if (!this.isEnabled) {
            return;
        }

        this.addonLifecycle.UnregisterListener(AddonEvent.PostSetup, TargetAddons, this.OnAddonSetup);
        this.addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, TargetAddons, this.OnAddonFinalize);
        this.framework.Update -= this.OnFrameworkUpdate;

        this.isEnabled = false;
        this.logger.Debug("SalesScannerService disabled.");
    }

    private void OnAddonSetup(AddonEvent type, AddonArgs args) {
        this.logger.Info("Retainer sales history UI opened. Starting continuous scan...");
        this.isScanActive = true;
        this.lastScanTime = DateTime.Now;
    }

    private void OnAddonFinalize(AddonEvent type, AddonArgs args) {
        this.logger.Info("Retainer sales history UI closed. Stopping continuous scan.");
        this.isScanActive = false;
    }

    private void OnFrameworkUpdate(IFramework frameworkInstance) {
        if (!this.isScanActive) {
            return;
        }

        // Continuously poll every 500ms to capture new rows as the user scrolls
        if ((DateTime.Now - this.lastScanTime).TotalMilliseconds < 500) {
            return;
        }

        this.lastScanTime = DateTime.Now;

        var sales = this.scraper.ScrapeSales();

        if (sales.Count > 0) {
            // The repository handles deduplication and saving internally
            this.repository.AddSales(sales);
        }
    }

    public void Dispose() {
        this.Disable();
    }
}