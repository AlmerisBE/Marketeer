using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class AutomationDelayServiceTests {
    [Fact]
    public void GetDelayMs_WhenDelayIsDisabled_ReturnsExactTechnicalMinimum() {
        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration { EnableAutomationDelay = false });

        var service = new AutomationDelayService(configService);

        int result = service.GetDelayMs(500);

        // Assert that the technical minimum is fully preserved to prevent UI crashes
        Assert.Equal(500, result);
    }

    [Fact]
    public void GetDelayMs_WhenDelayIsEnabled_ReturnsValueAboveTechnicalMinimum() {
        var configService = Substitute.For<IConfigurationService>();
        configService.GetConfig().Returns(new PluginConfiguration {
            EnableAutomationDelay = true,
            AutomationDelayMin = 1,
            AutomationDelayMax = 3
        });

        var service = new AutomationDelayService(configService);

        int result = service.GetDelayMs(500);

        // Assert that the humanized delay is actively stacked ON TOP of the technical 500ms minimum
        Assert.True(result >= 1500 && result <= 3500);
    }
}