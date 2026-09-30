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
        public DateTime CurrentTime { get; set; } = DateTime.Now;

        public TestableRetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger)
            : base(framework, uiInteraction, logger) { }

        protected override DateTime GetNow() => this.CurrentTime;
    }

    [Fact]
    public void SwitchTo_WhenAtRetainerList_SelectsRetainerAndInstantlyOpensMarket() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var logger = Substitute.For<ILoggerService>();

        var service = new TestableRetainerSwitcherService(framework, uiInteraction, logger);

        service.SwitchTo("MyTargetRetainer", true);

        // Using Reflection bypasses Dalamud delegate type changes and prevents CS0246 errors
        Action triggerUpdate = () => {
            var method = typeof(RetainerSwitcherService).GetMethod("OnFrameworkUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(service, new object[] { framework });
        };

        // State 0 -> 1: No secondary windows are open
        uiInteraction.IsAddonReady("RetainerSellList").Returns(false);
        uiInteraction.IsAddonReady("RetainerHistory").Returns(false);
        triggerUpdate();

        // State 1 -> 2: SelectString is closed, RetainerList is visible
        uiInteraction.IsAddonReady("SelectString").Returns(false);
        uiInteraction.IsAddonReady("RetainerList").Returns(true);
        triggerUpdate();

        // State 2: Interact with list
        triggerUpdate();
        uiInteraction.Received(1).SelectRetainer("MyTargetRetainer");

        // Game UI simulation: List closes, SelectString opens
        uiInteraction.IsAddonReady("RetainerList").Returns(false);
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        uiInteraction.IsMenuReadyForRetainer("MyTargetRetainer").Returns(true);

        // State 3: Menu validation and Market execution
        triggerUpdate();
        uiInteraction.Received(1).OpenRetainerMarket();

        // Validate service halted correctly
        triggerUpdate();
        uiInteraction.Received(1).OpenRetainerMarket(); // Should not increment
    }

    [Fact]
    public void SwitchTo_WhenWrongRetainerSummoned_BacksOutToRetainerListAndRetries() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var logger = Substitute.For<ILoggerService>();

        var service = new TestableRetainerSwitcherService(framework, uiInteraction, logger);

        service.SwitchTo("MyTargetRetainer", false);

        Action triggerUpdate = () => {
            var method = typeof(RetainerSwitcherService).GetMethod("OnFrameworkUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(service, new object[] { framework });
        };

        // Bypass State 0 directly into State 1
        uiInteraction.IsAddonReady("RetainerSellList").Returns(false);
        uiInteraction.IsAddonReady("RetainerHistory").Returns(false);
        triggerUpdate();

        // State 1: We are at SelectString, but the name doesn't match
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        uiInteraction.IsMenuReadyForRetainer("MyTargetRetainer").Returns(false);
        triggerUpdate();

        uiInteraction.Received(1).CloseSelectString();

        // Fast forward mock time to bypass the throttle intended to protect server network requests
        service.CurrentTime = service.CurrentTime.AddSeconds(1);

        // Game UI Simulation: Menu closed, back to List
        uiInteraction.IsAddonReady("SelectString").Returns(false);
        uiInteraction.IsAddonReady("RetainerList").Returns(true);
        triggerUpdate(); // Transition State 1 -> 2

        triggerUpdate(); // State 2: Select
        uiInteraction.Received(1).SelectRetainer("MyTargetRetainer");
    }
}