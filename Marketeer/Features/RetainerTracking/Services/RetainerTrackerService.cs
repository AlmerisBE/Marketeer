using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Financials.Models;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.RetainerTracking.Services;

public class RetainerTrackerService : IRetainerTrackerService, IDisposable {
    private IObjectTable objectTable;
    private IConfigurationService configService;
    private IRetainerProvider retainerProvider;
    private ICharacterTrackerService characterTrackerService;
    private IGameEventService gameEventService;
    private ILoggerService logger;

    public RetainerTrackerService(
        IObjectTable objectTable,
        IConfigurationService configService,
        IRetainerProvider retainerProvider,
        ICharacterTrackerService characterTrackerService,
        IGameEventService gameEventService,
        ILoggerService logger) {

        this.objectTable = objectTable;
        this.configService = configService;
        this.retainerProvider = retainerProvider;
        this.characterTrackerService = characterTrackerService;
        this.gameEventService = gameEventService;
        this.logger = logger;

        this.gameEventService.RetainerBellOpened += this.RecordRetainers;
        this.gameEventService.RetainerSellListUpdated += this.RecordRetainers;
    }

    public IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId) {
        var config = this.configService.GetConfig();
        var storageKey = $"{characterName}_{homeWorldId}";

        if (!config.FinancialRecords.TryGetValue(storageKey, out var charData)) {
            return new List<TrackedRetainer>();
        }

        return charData.Retainers.Values.Select(r => new TrackedRetainer {
            RetainerId = r.RetainerId,
            Name = r.Name,
            Gil = (uint)r.GilHeld,
            MarketItemCount = (uint)r.MarketListings.Count,
            AssociatedCharacterName = characterName,
            AssociatedHomeWorldId = homeWorldId
        }).ToList();
    }

    public void RecordRetainers() {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            return;
        }

        var activeRetainers = this.retainerProvider.GetActiveRetainers();
        if (activeRetainers.Count == 0) {
            return;
        }

        var config = this.configService.GetConfig();
        var storageKey = $"{localPlayer.Name.TextValue}_{localPlayer.HomeWorld.RowId}";

        lock (config) {
            if (!config.FinancialRecords.TryGetValue(storageKey, out var charData)) {
                return;
            }

            bool isModified = false;

            foreach (var retainer in activeRetainers) {
                if (!charData.Retainers.TryGetValue(retainer.RetainerId, out var existing)) {
                    existing = new RetainerFinancialData {
                        RetainerId = retainer.RetainerId,
                        Name = retainer.Name,
                        GilHeld = retainer.Gil
                    };
                    charData.Retainers[retainer.RetainerId] = existing;
                    isModified = true;
                }
                else {
                    if (existing.Name != retainer.Name || existing.GilHeld != retainer.Gil) {
                        existing.Name = retainer.Name;
                        existing.GilHeld = retainer.Gil;
                        isModified = true;
                    }
                }
            }

            if (isModified) {
                this.configService.Save();
            }
        }
    }

    public void Dispose() {
        this.gameEventService.RetainerBellOpened -= this.RecordRetainers;
        this.gameEventService.RetainerSellListUpdated -= this.RecordRetainers;
    }
}