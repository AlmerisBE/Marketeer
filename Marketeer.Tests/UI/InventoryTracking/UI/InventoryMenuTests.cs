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
        var config = Substitute.For<IConfigurationService>();
        var tracker = Substitute.For<ICharacterTrackerService>();
        var loc = Substitute.For<ILocalizationService>();
        var resolver = Substitute.For<IItemResolverService>();
        var texture = Substitute.For<ITextureProvider>();

        // Aligning mock with the new UI UX grouping strategy
        loc.Translate("CharacterList_TabName").Returns("Characters");
        loc.Translate("InventoryTab_Title").Returns("Inventory");

        var menu = new InventoryMenu(config, tracker, loc, resolver, texture);

        Assert.Equal("Characters", menu.GroupName);
        Assert.Equal("Inventory", menu.Name);
        Assert.Equal(50, menu.Priority);
    }
}