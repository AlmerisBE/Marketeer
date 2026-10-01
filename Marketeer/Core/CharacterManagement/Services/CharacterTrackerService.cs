using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.Logging.Contracts;
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
    private IInventoryService inventoryService;

    public event Action<string, uint>? CharacterForgotten;

    public CharacterTrackerService(
        IClientState clientState,
        IObjectTable objectTable,
        IConfigurationService configService,
        ILoggerService logger,
        IFramework framework,
        IInventoryService inventoryService) {

        this.clientState = clientState;
        this.objectTable = objectTable;
        this.configService = configService;
        this.logger = logger;
        this.framework = framework;
        this.inventoryService = inventoryService;

        this.clientState.Login += this.OnLogin;

        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) this.RecordCurrentCharacter();
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
            if (this.objectTable == null || this.configService == null || this.logger == null) return;

            var localPlayer = this.objectTable.LocalPlayer;
            if (localPlayer == null || localPlayer.Name == null) return;

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

            charData.CharacterGil = (ulong)this.inventoryService.GetItemCountInInventory(1);

            this.UpdatePlayerCrystals(storageKey);

            this.configService.Save();
        }
        catch (Exception ex) {
            this.logger?.Error(ex, "Failed to record current character safely.");
        }
    }

    private void UpdatePlayerCrystals(string storageKey) {
        var config = this.configService.GetConfig();
        if (!config.InventorySnapshots.TryGetValue(storageKey, out var snapshot)) {
            snapshot = new InventorySnapshot { Timestamp = DateTime.UtcNow, Items = new List<TrackedItem>() };
            config.InventorySnapshots[storageKey] = snapshot;
        }

        // Remove existing crystals (Item IDs 2 to 19 inclusive) to prevent duplication during partial updates
        snapshot.Items.RemoveAll(i => i.ItemId is >= 2 and <= 19);

        var crystalSlots = this.inventoryService.GetInventorySlots(InventoryType.Crystals);
        if (crystalSlots != null) {
            foreach (var slot in crystalSlots.Where(s => s.IsOccupied && s.Quantity > 0)) {
                snapshot.Items.Add(new TrackedItem {
                    ContainerId = (int)InventoryType.Crystals,
                    SlotIndex = (int)slot.SlotIndex,
                    ItemId = slot.ItemId,
                    Quantity = slot.Quantity
                });
            }
        }

        snapshot.Timestamp = DateTime.UtcNow;
    }

    public bool IsActiveCharacter(string name, uint homeWorldId) {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) return false;

        return localPlayer.Name.TextValue == name && localPlayer.HomeWorld.RowId == homeWorldId;
    }

    public void ForgetCharacter(string name, uint homeWorldId) {
        if (this.IsActiveCharacter(name, homeWorldId)) return;

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