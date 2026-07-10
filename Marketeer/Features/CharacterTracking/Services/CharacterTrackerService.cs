using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.CharacterTracking.Services;

public class CharacterTrackerService : ICharacterTrackerService, IDisposable {
    private IClientState clientState;
    private IObjectTable objectTable;
    private IConfigurationService configService;
    private ILoggerService logger;
    private IFramework framework;

    public CharacterTrackerService(
        IClientState clientState,
        IObjectTable objectTable,
        IConfigurationService configService,
        ILoggerService logger,
        IFramework framework) {

        this.clientState = clientState;
        this.objectTable = objectTable;
        this.configService = configService;
        this.logger = logger;
        this.framework = framework;

        this.clientState.Login += this.OnLogin;

        // Dispatch the initial check to the main framework thread.
        // This prevents "Not on main thread!" exceptions when hot-reloading the plugin while already logged in.
        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                this.RecordCurrentCharacter();
            }
        });
    }

    public IReadOnlyList<TrackedCharacter> GetKnownCharacters() {
        return this.configService.GetConfig().KnownCharacters;
    }

    public void RecordCurrentCharacter() {
        try {
            if (this.objectTable == null || this.configService == null || this.logger == null) {
                return;
            }

            var localPlayer = this.objectTable.LocalPlayer;

            if (localPlayer == null || localPlayer.Name == null) {
                return;
            }

            var characterName = localPlayer.Name.TextValue;
            if (string.IsNullOrWhiteSpace(characterName)) {
                return;
            }

            var worldId = localPlayer.HomeWorld.RowId;

            var currentCharacter = new TrackedCharacter {
                Name = characterName,
                HomeWorldId = worldId
            };

            var config = this.configService.GetConfig();
            config.KnownCharacters ??= new List<TrackedCharacter>();

            if (!config.KnownCharacters.Contains(currentCharacter)) {
                config.KnownCharacters.Add(currentCharacter);
                this.configService.Save();
                this.logger.Info($"New character recorded: {characterName} (World ID: {worldId})");
            }
        }
        catch (Exception ex) {
            this.logger?.Error(ex, "Failed to record current character safely.");
        }
    }

    public bool IsActiveCharacter(string name, uint homeWorldId) {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            return false;
        }

        return localPlayer.Name.TextValue == name && localPlayer.HomeWorld.RowId == homeWorldId;
    }

    public void ForgetCharacter(string name, uint homeWorldId) {
        if (this.IsActiveCharacter(name, homeWorldId)) {
            this.logger.Warning($"Attempted to forget active character {name}. Action aborted.");
            return;
        }

        var config = this.configService.GetConfig();
        config.KnownCharacters ??= new List<TrackedCharacter>();

        var targetCharacter = config.KnownCharacters.FirstOrDefault(c => c.Name == name && c.HomeWorldId == homeWorldId);

        if (targetCharacter != null) {
            config.KnownCharacters.Remove(targetCharacter);
            this.configService.Save();
            this.logger.Info($"Character forgotten manually: {name} (World ID: {homeWorldId})");
        }
    }

    private void OnLogin() {
        this.RecordCurrentCharacter();
    }

    public void Dispose() {
        this.clientState.Login -= this.OnLogin;
    }
}