using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using System.Reflection;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class RetainerSwitcherServiceTests {
    private class TestableRetainerSwitcherService : RetainerSwitcherService {
        public DateTime CurrentTime { get; set; } = DateTime.UtcNow;

        public TestableRetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger)
            : base(framework, uiInteraction, logger) { }

        protected override DateTime GetNow() => this.CurrentTime;
    }

    [Fact]
    public void SwitchTo_StatelessFlow_CompletesSuccessfully() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var logger = Substitute.For<ILoggerService>();

        var service = new TestableRetainerSwitcherService(framework, uiInteraction, logger);

        service.SwitchTo("MyTargetRetainer", true);

        Action triggerUpdate = () => {
            var method = typeof(RetainerSwitcherService).GetMethod("OnFrameworkUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(service, new object[] { framework });
        };

        // 1. Initial State: Only RetainerList is open
        uiInteraction.IsAddonReady(Arg.Any<string>()).Returns(false);
        uiInteraction.IsAddonReady("RetainerList").Returns(true);

        triggerUpdate();
        uiInteraction.Received(1).SelectRetainer("MyTargetRetainer");

        // 2. FFXIV UI shifts to SelectString natively
        uiInteraction.IsAddonReady("RetainerList").Returns(false);
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        uiInteraction.IsMenuReadyForRetainer("MyTargetRetainer").Returns(true);

        // Fast forward mock time to bypass throttle safely
        service.CurrentTime = service.CurrentTime.AddSeconds(1);

        triggerUpdate();
        uiInteraction.Received(1).OpenRetainerMarket();

        // 3. FFXIV UI shifts to Market
        uiInteraction.IsAddonReady("SelectString").Returns(false);
        uiInteraction.IsAddonReady("RetainerSellList").Returns(true);

        service.CurrentTime = service.CurrentTime.AddSeconds(1);

        triggerUpdate();

        // Assert no more unexpected clicks happened and automation concluded
        uiInteraction.Received(1).OpenRetainerMarket();
    }
}