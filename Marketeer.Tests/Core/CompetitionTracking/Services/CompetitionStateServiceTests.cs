using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.UI.CompetitionTracking.Models;
using Xunit;

namespace Marketeer.Tests.Core.CompetitionTracking.Services;

public class CompetitionStateServiceTests {

    [Fact]
    public void GetUndercutItems_InitialState_ReturnsEmptyList() {
        // Arrange
        var service = new CompetitionStateService();

        // Act
        var items = service.GetUndercutItems();

        // Assert
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void UpdateUndercuts_WithNewData_UpdatesStateSuccessfully() {
        // Arrange
        var service = new CompetitionStateService();
        var undercuts = new List<UndercutItem> {
            new UndercutItem { ItemId = 1, ItemName = "Potion", OurPrice = 100, ServerCheapestPrice = 90 }
        };

        // Act
        service.UpdateUndercuts(undercuts);
        var retrievedItems = service.GetUndercutItems();

        // Assert
        Assert.Single(retrievedItems);
        Assert.Equal("Potion", retrievedItems[0].ItemName);
        Assert.Equal(100u, retrievedItems[0].OurPrice);
    }

    // Ajoute ce test à la suite existante
    [Fact]
    public void UpdateItemUndercuts_ModifiesOnlySpecificItemAndPreservesOthers() {
        // Arrange
        var service = new CompetitionStateService();
        service.UpdateUndercuts(new List<UndercutItem> {
            new() { ItemId = 10, ItemName = "Potion" },
            new() { ItemId = 20, ItemName = "Ether" }
        });

        var newPotionUndercuts = new List<UndercutItem> {
            new() { ItemId = 10, ItemName = "Potion (Updated)" }
        };

        // Act
        service.UpdateItemUndercuts(10, newPotionUndercuts);
        var retrievedItems = service.GetUndercutItems();

        // Assert
        Assert.Equal(2, retrievedItems.Count);
        Assert.Contains(retrievedItems, i => i.ItemId == 10 && i.ItemName == "Potion (Updated)");
        Assert.Contains(retrievedItems, i => i.ItemId == 20 && i.ItemName == "Ether");
    }
}