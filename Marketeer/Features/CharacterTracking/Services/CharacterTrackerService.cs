using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Logging.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Services;

public class CharacterTrackerService : ICharacterTrackerService, IDisposable {
    private IClientState clientState;
    private IObjectTable objectTable;
    private IConfigurationService configService;
    private ILoggerService logger;

    public CharacterTrackerService(
        IClientState clientState,
        IObjectTable objectTable,
        IConfigurationService configService,
        ILoggerService logger) {

        this.clientState = clientState;
        this.objectTable = objectTable;
        this.configService = configService;
        this.logger = logger;

        this.clientState.Login += this.OnLogin;

        this.RecordCurrentCharacter();
    }

    public IReadOnlyList<TrackedCharacter> GetKnownCharacters() {
        return this.configService.GetConfig().KnownCharacters;
    }

    public void RecordCurrentCharacter() {
        var localPlayer = this.objectTable.LocalPlayer;

        if (localPlayer == null) {
            return;
        }

        var characterName = localPlayer.Name.TextValue;
        var worldId = localPlayer.HomeWorld.RowId;

        var currentCharacter = new TrackedCharacter {
            Name = characterName,
            HomeWorldId = worldId
        };

        var config = this.configService.GetConfig();

        if (!config.KnownCharacters.Contains(currentCharacter)) {
            config.KnownCharacters.Add(currentCharacter);
            this.configService.Save();
            this.logger.Info($"New character recorded: {characterName} (World ID: {worldId})");
        }
    }

    private void OnLogin() {
        this.RecordCurrentCharacter();
    }

    public void Dispose() {
        this.clientState.Login -= this.OnLogin;
    }
}