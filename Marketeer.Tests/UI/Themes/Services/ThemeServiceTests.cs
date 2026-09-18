using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Models;
using Marketeer.UI.Themes.Services;
using NSubstitute;
using System.Numerics;
using Xunit;

namespace Marketeer.Tests.UI.Themes.Services;

public class ThemeServiceTests {
    [Fact]
    public void GetCustomColor_WhenHexProvided_ShouldReturnCorrectVector4() {
        var repo = Substitute.For<IThemeRepository>();
        var config = Substitute.For<IConfigurationService>();
        var logger = Substitute.For<ILoggerService>();

        config.GetConfig().Returns(new PluginConfiguration { SelectedTheme = "Dark" });

        var theme = new ThemeDefinition {
            Name = "Dark",
            Palette = new Dictionary<string, string> {
                { "TextOnline", "#FF0000FF" } // Red, full opacity
            }
        };

        repo.GetTheme("Dark").Returns(theme);

        var service = new ThemeService(repo, config, logger);

        var color = service.GetCustomColor("TextOnline", Vector4.Zero);

        Assert.Equal(1.0f, color.X); // R
        Assert.Equal(0.0f, color.Y); // G
        Assert.Equal(0.0f, color.Z); // B
        Assert.Equal(1.0f, color.W); // A
    }
}