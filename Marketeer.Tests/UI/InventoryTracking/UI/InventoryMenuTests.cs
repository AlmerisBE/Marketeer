using Dalamud.Plugin.Services;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.InventoryBrowser.UI;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.InventoryTracking.UI;

public class InventoryMenuTests {
    [Fact]
    public void Properties_ShouldReturnExpectedValuesFromLocalization() {
        var mockConfig = Substitute.For<IConfigurationService>();
        var mockCharacterTracker = Substitute.For<ICharacterTrackerService>();
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockResolver = Substitute.For<IItemResolverService>();
        var mockTexture = Substitute.For<ITextureProvider>();

        mockLocalization.Translate("Group_Inventory").Returns("Inventories");
        mockLocalization.Translate("InventoryTab_Title").Returns("Bags & Retainers");

        var menu = new InventoryMenu(mockConfig, mockCharacterTracker, mockLocalization, mockResolver, mockTexture);

        Assert.Equal("Inventories", menu.GroupName);
        Assert.Equal("Bags & Retainers", menu.Name);
        Assert.Equal(15, menu.Priority);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}