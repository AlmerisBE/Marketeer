using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.MarketListings.Contracts;
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
        string? activeRetainerName = null;

        if (activeId.HasValue) {
            var retainers = this.retainerProvider.GetActiveRetainers();
            var activeRetainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);
            activeRetainerName = activeRetainer?.Name;
        }

        var allUndercuts = this.competitionState.GetUndercutItems().OrderByDescending(u => u.OurPrice).ToList();
        var allSuboptimal = this.optimizationService.GetVendorPricedListings().OrderByDescending(s => s.VendorPrice).ToList();

        // 1. Evaluate instructions for the CURRENT retainer first
        if (!string.IsNullOrEmpty(activeRetainerName)) {
            var currentUndercut = allUndercuts.FirstOrDefault(u => u.RetainerName == activeRetainerName);
            if (currentUndercut != null) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.UpdatePrice,
                    ItemName = currentUndercut.ItemName,
                    TargetPrice = currentUndercut.TargetPrice,
                    RetainerName = activeRetainerName
                };
            }

            var currentSuboptimal = allSuboptimal.FirstOrDefault(s => s.RetainerName == activeRetainerName);
            if (currentSuboptimal != null) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.CancelListing,
                    ItemName = currentSuboptimal.ItemName,
                    RetainerName = activeRetainerName
                };
            }
        }

        // 2. If the current retainer is fully optimized (or no retainer is active), suggest the best global switch
        if (allUndercuts.Count > 0) {
            return new GuidanceInstruction {
                ActionType = GuidanceActionType.SwitchRetainer,
                RetainerName = allUndercuts[0].RetainerName
            };
        }

        if (allSuboptimal.Count > 0) {
            return new GuidanceInstruction {
                ActionType = GuidanceActionType.SwitchRetainer,
                RetainerName = allSuboptimal[0].RetainerName
            };
        }

        return null;
    }
}