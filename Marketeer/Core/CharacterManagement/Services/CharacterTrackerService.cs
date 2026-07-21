using Dalamud.Plugin.Services;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Financials.Models;
using Marketeer.API.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.CharacterManagement.Services;

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
            HomeWorldId = c.HomeWorldId,
            CompanyTag = c.CompanyTag,
            LastScanDate = c.LastScanDate,
            RetainerCount = c.Retainers.Count
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
            var companyTag = localPlayer.CompanyTag.TextValue ?? string.Empty;

            var config = this.configService.GetConfig();

            if (!config.FinancialRecords.TryGetValue(storageKey, out var charData)) {
                charData = new CharacterFinancialData {
                    CharacterName = characterName,
                    HomeWorldId = worldId,
                };
                config.FinancialRecords[storageKey] = charData;
            }

            charData.CompanyTag = companyTag;
            charData.LastScanDate = DateTime.UtcNow;

            this.configService.Save();
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