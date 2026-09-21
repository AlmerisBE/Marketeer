using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.UI;

public class AboutMenuTests {
    [Fact]
    public void Constructor_ShouldInitializeNavigationNodeProperly() {
        var localization = Substitute.For<ILocalizationService>();

        localization.Translate("About_TabName").Returns("About");
        localization.Translate("Group_General").Returns("General");

        var menu = new AboutMenu(localization);

        Assert.IsAssignableFrom<INavigationNode>(menu);
        Assert.Equal("About", menu.Name);
        Assert.Equal("General", menu.GroupName);
        Assert.Equal(5, menu.Priority);
        Assert.True(menu.HasContent);
        Assert.False(menu.DefaultExpanded);
        Assert.Empty(menu.GetChildren());
    }
}