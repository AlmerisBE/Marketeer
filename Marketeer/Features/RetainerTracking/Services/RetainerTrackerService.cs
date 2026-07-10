using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.Configuration.Contracts;
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
    private ILoggerService logger;

    public RetainerTrackerService(
        IObjectTable objectTable,
        IConfigurationService configService,
        IRetainerProvider retainerProvider,
        ICharacterTrackerService characterTrackerService,
        ILoggerService logger) {

        this.objectTable = objectTable;
        this.configService = configService;
        this.retainerProvider = retainerProvider;
        this.characterTrackerService = characterTrackerService;
        this.logger = logger;

        // Subscribe to the character tracking event to handle cascading deletes
        this.characterTrackerService.CharacterForgotten += this.OnCharacterForgotten;
    }

    public IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId) {
        var config = this.configService.GetConfig();
        config.KnownRetainers ??= new List<TrackedRetainer>();

        return config.KnownRetainers
            .Where(r => r.AssociatedCharacterName == characterName && r.AssociatedHomeWorldId == homeWorldId)
            .ToList();
    }

    public void RecordRetainers() {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            return;
        }

        var characterName = localPlayer.Name.TextValue;
        var worldId = localPlayer.HomeWorld.RowId;

        // Read unmanaged memory
        var activeRetainers = this.retainerProvider.GetActiveRetainers();
        if (activeRetainers.Count == 0) {
            return;
        }

        var config = this.configService.GetConfig();
        config.KnownRetainers ??= new List<TrackedRetainer>();

        bool isModified = false;

        foreach (var retainer in activeRetainers) {
            // Assign foreign keys
            retainer.AssociatedCharacterName = characterName;
            retainer.AssociatedHomeWorldId = worldId;

            // Check for existing retainer to avoid duplicates, but update name if it changed
            var existing = config.KnownRetainers.FirstOrDefault(r => r.RetainerId == retainer.RetainerId);

            if (existing != null) {
                if (existing.Name != retainer.Name) {
                    existing.Name = retainer.Name;
                    isModified = true;
                }
            }
            else {
                config.KnownRetainers.Add(retainer);
                isModified = true;
                this.logger.Info($"New retainer recorded: {retainer.Name} (Owner: {characterName})");
            }
        }

        if (isModified) {
            this.configService.Save();
        }
    }

    private void OnCharacterForgotten(string characterName, uint homeWorldId) {
        var config = this.configService.GetConfig();
        config.KnownRetainers ??= new List<TrackedRetainer>();

        var initialCount = config.KnownRetainers.Count;

        // Remove all retainers matching the foreign keys
        config.KnownRetainers.RemoveAll(r => r.AssociatedCharacterName == characterName && r.AssociatedHomeWorldId == homeWorldId);

        if (config.KnownRetainers.Count < initialCount) {
            this.configService.Save();
            this.logger.Info($"Cascading delete executed: Retainers for {characterName} were removed.");
        }
    }

    public void Dispose() {
        this.characterTrackerService.CharacterForgotten -= this.OnCharacterForgotten;
    }
}