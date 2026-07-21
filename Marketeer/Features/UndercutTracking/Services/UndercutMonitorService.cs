using Dalamud.Plugin.Services;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marketeer.Features.UndercutTracking.Services;

public class UndercutMonitorService : IUndercutMonitorService {
    private IRetainerStateService retainerState;
    private IServerPriceProvider priceProvider;
    private ICompetitionStateService competitionState;
    private IItemResolverService itemResolver;
    private IClientState clientState;
    private IObjectTable objectTable;
    private IChatGui chatGui;
    private ILocalizationService localizationService;
    private ILoggerService logger;
    private IFramework framework;

    private CancellationTokenSource? cancellationTokenSource;
    private Task? monitorTask;
    private TimeSpan checkInterval = TimeSpan.FromMinutes(10);

    public UndercutMonitorService(
        IRetainerStateService retainerState,
        IServerPriceProvider priceProvider,
        ICompetitionStateService competitionState,
        IItemResolverService itemResolver,
        IClientState clientState,
        IObjectTable objectTable,
        IChatGui chatGui,
        ILocalizationService localizationService,
        ILoggerService logger,
        IFramework framework) {

        this.retainerState = retainerState;
        this.priceProvider = priceProvider;
        this.competitionState = competitionState;
        this.itemResolver = itemResolver;
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.chatGui = chatGui;
        this.localizationService = localizationService;
        this.logger = logger;
        this.framework = framework;

        this.clientState.Login += this.OnLogin;
    }

    public void StartMonitoring() {
        if (this.cancellationTokenSource != null) {
            return;
        }

        this.logger.Info("Starting undercut monitoring service...");
        this.cancellationTokenSource = new CancellationTokenSource();
        this.monitorTask = Task.Run(() => this.MonitorLoopAsync(this.cancellationTokenSource.Token));
    }

    public void StopMonitoring() {
        if (this.cancellationTokenSource == null) {
            return;
        }

        this.logger.Info("Stopping undercut monitoring service...");
        this.cancellationTokenSource.Cancel();
        this.monitorTask?.Wait();
        this.cancellationTokenSource.Dispose();
        this.cancellationTokenSource = null;
    }

    private void OnLogin() {
        this.logger.Info("Player logged in. Triggering immediate undercut check.");
        Task.Run(async () => await this.CheckUndercutsAsync());
    }

    private async Task MonitorLoopAsync(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            try {
                await this.CheckUndercutsAsync();
            }
            catch (Exception ex) {
                this.logger.Error(ex, "Unexpected error in undercut monitoring loop.");
            }

            try {
                await Task.Delay(this.checkInterval, token);
            }
            catch (TaskCanceledException) {
                break;
            }
        }
    }

    public async Task CheckUndercutsAsync() {
        uint currentWorldId = 0;
        bool canProceed = false;

        await this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn && this.objectTable.LocalPlayer != null) {
                currentWorldId = this.objectTable.LocalPlayer.CurrentWorld.RowId;
                canProceed = true;
            }
        });

        if (!canProceed || currentWorldId == 0) {
            return;
        }

        IReadOnlyList<RetainerListing> currentListings = System.Array.Empty<RetainerListing>();

        await this.framework.RunOnFrameworkThread(() => {
            currentListings = this.retainerState.GetCurrentListings();
        });

        if (currentListings.Count == 0) {
            return;
        }

        var itemIds = currentListings.Select(l => l.ItemId).Distinct().ToList();
        var serverPrices = await this.priceProvider.GetLowestPricesAsync(itemIds, currentWorldId);

        var detectedUndercuts = new List<UndercutItem>();

        foreach (var listing in currentListings) {
            var lowestPriceData = serverPrices.FirstOrDefault(p => p.ItemId == listing.ItemId);

            if (lowestPriceData != null && lowestPriceData.Price < listing.CurrentPrice) {
                detectedUndercuts.Add(new UndercutItem {
                    ItemId = listing.ItemId,
                    ItemName = this.itemResolver.ResolveItemName(listing.ItemId),
                    RetainerName = listing.RetainerName,
                    OurPrice = listing.CurrentPrice,
                    ServerCheapestPrice = lowestPriceData.Price
                });
            }
        }

        this.competitionState.UpdateUndercuts(detectedUndercuts);

        if (detectedUndercuts.Count > 0) {
            var message = this.localizationService.Translate("Undercuts_Notification", detectedUndercuts.Count);
            this.chatGui.Print(message);
        }
    }

    public void Dispose() {
        this.clientState.Login -= this.OnLogin;
        this.StopMonitoring();
    }
}