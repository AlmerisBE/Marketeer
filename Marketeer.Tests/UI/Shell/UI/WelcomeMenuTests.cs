using Dalamud.Plugin;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.UI;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.Shell.UI;

public class WelcomeMenuTests {
    [Fact]
    public void DrawContent_RequestsAllLocalizedSectionDescriptions() {
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var localization = Substitute.For<ILocalizationService>();

        // Utilizing NSubstitute's deep mocking to bypass explicit manifestation of Dalamud's internal types
        pluginInterface.Manifest.AssemblyVersion.Returns(new Version(1, 0, 0, 0));

        var menu = new WelcomeMenu(pluginInterface, localization);

        // Act
        menu.DrawContent();

        // Assert
        localization.Received(1).Translate("Welcome_Sec_Guidance_Title");
        localization.Received(1).Translate("Welcome_Sec_Guidance_Desc");
        localization.Received(1).Translate("Welcome_Sec_Chars_Title");
        localization.Received(1).Translate("Welcome_Sec_Config_Desc");
    }
}