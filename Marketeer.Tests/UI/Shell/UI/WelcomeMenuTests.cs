using Dalamud.Plugin;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.UI;

public class WelcomeMenuTests {
    [Fact]
    public void Sections_ContainsAllExpectedLocalizationKeys() {
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var localization = Substitute.For<ILocalizationService>();

        pluginInterface.Manifest.AssemblyVersion.Returns(new Version(1, 0, 0, 0));

        var menu = new WelcomeMenu(pluginInterface, localization);

        // Act
        var sections = menu.Sections;

        // Assert
        Assert.Contains(sections, s => s.TitleKey == "Welcome_Sec_Guidance_Title" && s.DescKey == "Welcome_Sec_Guidance_Desc");
        Assert.Contains(sections, s => s.TitleKey == "Welcome_Sec_Chars_Title" && s.DescKey == "Welcome_Sec_Chars_Desc");
        Assert.Contains(sections, s => s.TitleKey == "Welcome_Sec_Sales_Title" && s.DescKey == "Welcome_Sec_Sales_Desc");
        Assert.Contains(sections, s => s.TitleKey == "Welcome_Sec_Config_Title" && s.DescKey == "Welcome_Sec_Config_Desc");

        Assert.Equal(7, sections.Count);
    }
}