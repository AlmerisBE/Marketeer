using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.UI.InventoryTracking.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.InventoryTracking.UI;

public class InventoryMenuTests {
    [Fact]
    public void Properties_ShouldReturnExpectedValuesFromLocalization() {
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockTexture = Substitute.For<ITextureProvider>();

        mockLocalization.Translate("Group_Inventory").Returns("Inventories");
        mockLocalization.Translate("InventoryTab_Title").Returns("Bags & Retainers");

        var menu = new InventoryMenu(mockConfig, mockLocalization, mockResolver, mockTexture);

        Assert.Equal("Inventories", menu.GroupName);
        Assert.Equal("Bags & Retainers", menu.Name);
        Assert.Equal(15, menu.Priority);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}