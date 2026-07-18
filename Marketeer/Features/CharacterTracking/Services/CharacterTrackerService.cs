using Dalamud.Plugin.Services;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Financials.Models;
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

    public event Action<string, uint>? CharacterForgotten;

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

        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                this.RecordCurrentCharacter();
            }
        });
    }

    public IReadOnlyList<TrackedCharacter> GetKnownCharacters() {
        var config = this.configService.GetConfig();
        return config.FinancialRecords.Values.Select(c => new TrackedCharacter {
            Name = c.CharacterName,
            HomeWorldId = c.HomeWorldId
        }).ToList();
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
            var worldId = localPlayer.HomeWorld.RowId;
            var storageKey = $"{characterName}_{worldId}";

            var config = this.configService.GetConfig();

            if (!config.FinancialRecords.ContainsKey(storageKey)) {
                config.FinancialRecords[storageKey] = new CharacterFinancialData {
                    CharacterName = characterName,
                    HomeWorldId = worldId
                };
                this.configService.Save();
                this.logger.Info($"New character recorded: {characterName} ({worldId})");
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
            return;
        }

        var storageKey = $"{name}_{homeWorldId}";
        var config = this.configService.GetConfig();

        if (config.FinancialRecords.Remove(storageKey)) {
            this.configService.Save();
            this.CharacterForgotten?.Invoke(name, homeWorldId);
        }
    }

    private void OnLogin() {
        this.RecordCurrentCharacter();
    }

    public void Dispose() {
        this.clientState.Login -= this.OnLogin;
    }
}