using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.MarketListings.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.Guidance.Services;

public class GuidanceEngineService : IGuidanceInstructionProvider, IDisposable {
    private IGameEventService gameEventService;
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;

    private GuidanceInstruction? currentInstruction;

    public GuidanceEngineService(
        IGameEventService gameEventService,
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService) {

        this.gameEventService = gameEventService;
        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;

        // Reactive triggers
        this.gameEventService.RetainerSellListUpdated += this.EvaluateState;
        this.gameEventService.RetainerBellOpened += this.ClearState;
    }

    public GuidanceInstruction? GetCurrentInstruction() => this.currentInstruction;

    private void EvaluateState() {
        var activeId = this.listingProvider.GetActiveRetainerId();
        if (!activeId.HasValue) {
            this.currentInstruction = null;
            return;
        }

        var retainers = this.retainerProvider.GetActiveRetainers();
        var activeRetainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);
        if (activeRetainer == null) {
            this.currentInstruction = null;
            return;
        }

        // Priorité 1 : Changement de prix (Plus impactant d'abord : tri par OurPrice décroissant)
        var undercuts = this.competitionState.GetUndercutItems()
            .Where(u => u.RetainerName == activeRetainer.Name)
            .OrderByDescending(u => u.OurPrice)
            .ToList();

        if (undercuts.Any()) {
            var top = undercuts.First();
            this.currentInstruction = new GuidanceInstruction {
                ActionType = GuidanceActionType.UpdatePrice,
                ItemName = top.ItemName,
                TargetPrice = Math.Max(1u, top.ServerCheapestPrice - 1)
            };
            return;
        }

        // Priorité 2 : Annulation de vente (Prix marchand le plus élevé d'abord : tri par VendorPrice décroissant)
        var suboptimal = this.optimizationService.GetVendorPricedListings()
            .Where(s => s.RetainerName == activeRetainer.Name)
            .OrderByDescending(s => s.VendorPrice)
            .ToList();

        if (suboptimal.Any()) {
            var top = suboptimal.First();
            this.currentInstruction = new GuidanceInstruction {
                ActionType = GuidanceActionType.CancelListing,
                ItemName = top.ItemName
            };
            return;
        }

        this.currentInstruction = null;
    }

    private void ClearState() {
        this.currentInstruction = null;
    }

    public void Dispose() {
        this.gameEventService.RetainerSellListUpdated -= this.EvaluateState;
        this.gameEventService.RetainerBellOpened -= this.ClearState;
    }
}