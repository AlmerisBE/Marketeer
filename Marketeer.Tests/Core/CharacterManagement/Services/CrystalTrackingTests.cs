using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Models;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CharacterManagement.Services;

public class CrystalTrackingTests {
    [Fact]
    public void RecordCurrentCharacter_UpdatesCrystalInventory_WithoutOverwritingRegularBags() {
        var clientState = Substitute.For<IClientState>();
        var objectTable = Substitute.For<IObjectTable>();
        var configService = Substitute.For<IConfigurationService>();
        var logger = Substitute.For<ILoggerService>();
        var framework = Substitute.For<IFramework>();
        var inventoryService = Substitute.For<IInventoryService>();

        var config = new PluginConfiguration();
        configService.GetConfig().Returns(config);

        // Define a pre-existing inventory snapshot with standard bag items (IDs > 19)
        var storageKey = "TestPlayer_0";
        config.InventorySnapshots[storageKey] = new InventorySnapshot {
            Items = new List<TrackedItem> {
                new TrackedItem { ItemId = 5000, Quantity = 1 } // Standard Item
            }
        };

        var localPlayer = Substitute.For<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();

        // Mock required SeString properties to prevent NullReferenceExceptions during extraction
        localPlayer.Name.Returns(new SeString(new TextPayload("TestPlayer")));
        localPlayer.CompanyTag.Returns(new SeString(new TextPayload("")));

        // Mock HomeWorld using Lumina's native RowRef struct representation
        localPlayer.HomeWorld.Returns(new RowRef<World>());

        objectTable.LocalPlayer.Returns(localPlayer);

        var mockSlot = new InventorySlotInfo {
            IsOccupied = true,
            ItemId = 2u,
            Quantity = 500u,
            SlotIndex = 0
        };

        inventoryService.GetInventorySlots(InventoryType.Crystals).Returns(new List<InventorySlotInfo> { mockSlot });

        var service = new CharacterTrackerService(clientState, objectTable, configService, logger, framework, inventoryService);

        // Act
        service.RecordCurrentCharacter();

        // Assert
        var snapshot = config.InventorySnapshots[storageKey];

        Assert.Equal(2, snapshot.Items.Count);
        Assert.Contains(snapshot.Items, i => i.ItemId == 5000);
        Assert.Contains(snapshot.Items, i => i.ItemId == 2 && i.Quantity == 500);
    }
}