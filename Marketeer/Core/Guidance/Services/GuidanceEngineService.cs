using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.MarketListings.Contracts;
using System;
using System.Linq;

namespace Marketeer.Core.Guidance.Services;

public class GuidanceEngineService : IGuidanceInstructionProvider {
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;

    public GuidanceEngineService(
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService) {

        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
    }

    public GuidanceInstruction? GetCurrentInstruction() {
        var activeId = this.listingProvider.GetActiveRetainerId();
        if (!activeId.HasValue) {
            return null;
        }

        var retainers = this.retainerProvider.GetActiveRetainers();
        var activeRetainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);
        if (activeRetainer == null) {
            return null;
        }

        // Priority 1: Most impactful price update first (Descending OurPrice)
        var undercuts = this.competitionState.GetUndercutItems()
            .Where(u => u.RetainerName == activeRetainer.Name)
            .OrderByDescending(u => u.OurPrice)
            .ToList();

        if (undercuts.Count > 0) {
            var top = undercuts[0];
            return new GuidanceInstruction {
                ActionType = GuidanceActionType.UpdatePrice,
                ItemName = top.ItemName,
                TargetPrice = Math.Max(1u, top.ServerCheapestPrice - 1)
            };
        }

        // Priority 2: Most impactful vendor cancellation first (Descending VendorPrice)
        var suboptimal = this.optimizationService.GetVendorPricedListings()
            .Where(s => s.RetainerName == activeRetainer.Name)
            .OrderByDescending(s => s.VendorPrice)
            .ToList();

        if (suboptimal.Count > 0) {
            var top = suboptimal[0];
            return new GuidanceInstruction {
                ActionType = GuidanceActionType.CancelListing,
                ItemName = top.ItemName
            };
        }

        return null;
    }
}