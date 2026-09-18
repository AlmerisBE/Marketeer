using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.UI.RetainerOverlays.Contracts;
using Marketeer.UI.RetainerOverlays.Models;
using Marketeer.UI.UiInterop.Contracts;
using System.Linq;

namespace Marketeer.UI.RetainerOverlays.Services;

public class GuidanceEngineService : IGuidanceInstructionProvider {
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private IObjectTable objectTable;
    private INativeWindowService windowService;

    public GuidanceEngineService(
        IMarketListingProvider listingProvider,
        IRetainerProvider retainerProvider,
        ICompetitionStateService competitionState,
        IListingOptimizationService optimizationService,
        IObjectTable objectTable,
        INativeWindowService windowService) {

        this.listingProvider = listingProvider;
        this.retainerProvider = retainerProvider;
        this.competitionState = competitionState;
        this.optimizationService = optimizationService;
        this.objectTable = objectTable;
        this.windowService = windowService;
    }

    public GuidanceInstruction? GetCurrentInstruction() {
        var activeId = this.listingProvider.GetActiveRetainerId();
        string? activeRetainerName = null;

        if (activeId.HasValue) {
            var retainers = this.retainerProvider.GetActiveRetainers();
            var activeRetainer = retainers.FirstOrDefault(r => r.RetainerId == activeId.Value);
            activeRetainerName = activeRetainer?.Name;
        }

        var retainerListWindow = this.windowService.GetWindow("RetainerList");
        bool isAtRetainerList = retainerListWindow != null && retainerListWindow.IsVisible;

        var allUndercuts = this.competitionState.GetUndercutItems().OrderByDescending(u => u.OurPrice).ToList();
        var allSuboptimal = this.optimizationService.GetVendorPricedListings().OrderByDescending(s => s.VendorPrice).ToList();

        var localPlayer = this.objectTable.LocalPlayer;
        string currentCharacterName = localPlayer?.Name.TextValue ?? string.Empty;

        // 1. Evaluate instructions for the CURRENT active retainer first (ONLY if we are not looking at the global list)
        if (!isAtRetainerList && !string.IsNullOrEmpty(activeRetainerName)) {
            var currentUndercut = allUndercuts.FirstOrDefault(u => u.RetainerName == activeRetainerName && u.CharacterName == currentCharacterName);
            if (currentUndercut != null) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.UpdatePrice,
                    ItemId = currentUndercut.ItemId,
                    ItemName = currentUndercut.ItemName,
                    CurrentPrice = currentUndercut.Price,
                    Quantity = currentUndercut.Quantity,
                    TargetPrice = currentUndercut.TargetPrice,
                    RetainerName = activeRetainerName,
                    CharacterName = currentCharacterName
                };
            }

            var currentSuboptimal = allSuboptimal.FirstOrDefault(s => s.RetainerName == activeRetainerName && s.CharacterName == currentCharacterName);
            if (currentSuboptimal != null) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.CancelListing,
                    ItemId = currentSuboptimal.ItemId,
                    ItemName = currentSuboptimal.ItemName,
                    CurrentPrice = currentSuboptimal.Price,
                    Quantity = currentSuboptimal.Quantity,
                    RetainerName = activeRetainerName,
                    CharacterName = currentCharacterName
                };
            }
        }

        // 2. Find the next target globally, prioritizing the currently logged-in character
        UndercutItem? targetUndercut = allUndercuts.FirstOrDefault(u => u.CharacterName == currentCharacterName);
        SuboptimalListing? targetSuboptimal = allSuboptimal.FirstOrDefault(s => s.CharacterName == currentCharacterName);

        if (targetUndercut == null && targetSuboptimal == null) {
            targetUndercut = allUndercuts.FirstOrDefault();
            targetSuboptimal = allSuboptimal.FirstOrDefault();
        }

        string? targetRetainer = targetUndercut?.RetainerName ?? targetSuboptimal?.RetainerName;
        string? targetCharacter = targetUndercut?.CharacterName ?? targetSuboptimal?.CharacterName;

        if (targetRetainer != null && targetCharacter != null) {
            if (targetCharacter != currentCharacterName) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.SwitchCharacter,
                    CharacterName = targetCharacter
                };
            }

            if (string.IsNullOrEmpty(activeRetainerName) || isAtRetainerList) {
                return new GuidanceInstruction {
                    ActionType = GuidanceActionType.SummonRetainer,
                    RetainerName = targetRetainer,
                    CharacterName = targetCharacter
                };
            }

            return new GuidanceInstruction {
                ActionType = GuidanceActionType.SwitchRetainer,
                RetainerName = targetRetainer,
                CharacterName = targetCharacter
            };
        }

        // 3. No target means everything is perfectly optimized across all tracked characters
        return null;
    }
}