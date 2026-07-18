using Marketeer.Features.Inventory;
using Marketeer.Features.Inventory.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.Inventory;

public class InventoryFeatureTests {
    [Fact]
    public void InventoryFeature_RegisterServices_ResolvesSuccessfully() {
        // Arrange
        var services = new ServiceCollection();
        var feature = new InventoryFeature();

        // Act
        feature.RegisterServices(services);
        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetService<IInventoryService>();

        // Assert
        Assert.NotNull(resolvedService);
    }

    [Fact]
    public void IInventoryService_Mocking_DemonstrationForTDD() {
        // Arrange
        var mockInventory = Substitute.For<IInventoryService>();
        uint dummyItemId = 12345;

        mockInventory.GetItemCountInInventory(dummyItemId).Returns(99);

        // Act
        var result = mockInventory.GetItemCountInInventory(dummyItemId);

        // Assert
        Assert.Equal(99, result);
    }
}