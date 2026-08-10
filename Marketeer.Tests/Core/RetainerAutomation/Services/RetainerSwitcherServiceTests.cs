using Dalamud.Plugin.Services;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class RetainerSwitcherServiceTests {
    private class TestableRetainerSwitcherService : RetainerSwitcherService {
        public DateTime MockNow { get; set; } = DateTime.Now;

        public TestableRetainerSwitcherService(IFramework framework, IRetainerUiInteractionService uiInteraction, ILoggerService logger)
            : base(framework, uiInteraction, logger) { }

        protected override DateTime GetNow() => this.MockNow;
    }

    [Fact]
    public void SwitchTo_CascadesWindowClosures_SelectsRetainer_AndOpensMarketIfRequested() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var logger = Substitute.For<ILoggerService>();

        // Ensure mock methods return true so the state machine can advance
        uiInteraction.CloseRetainerMarket().Returns(true);
        uiInteraction.CloseSalesHistory().Returns(true);
        uiInteraction.CloseSelectString().Returns(true);
        uiInteraction.SelectRetainer(Arg.Any<string>()).Returns(true);

        IFramework.OnUpdateDelegate? updateCallback = null;
        framework.When(x => x.Update += Arg.Any<IFramework.OnUpdateDelegate>())
            .Do(x => updateCallback = x.Arg<IFramework.OnUpdateDelegate>());

        using var service = new TestableRetainerSwitcherService(framework, uiInteraction, logger);

        service.SwitchTo("MyRetainer", openMarketList: true);

        // Tick 1: Retainer market is open, should close it
        uiInteraction.IsAddonReady("RetainerSellList").Returns(true);
        updateCallback?.Invoke(framework);
        uiInteraction.Received(1).CloseRetainerMarket();

        service.MockNow = service.MockNow.AddSeconds(1);

        // Tick 2: Retainer menu is open, should close it
        uiInteraction.IsAddonReady("RetainerSellList").Returns(false);
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        updateCallback?.Invoke(framework);
        uiInteraction.Received(1).CloseSelectString();

        service.MockNow = service.MockNow.AddSeconds(1);

        // Tick 3: At Retainer List, should summon retainer
        uiInteraction.IsAddonReady("SelectString").Returns(false);
        uiInteraction.IsAddonReady("RetainerList").Returns(true);
        updateCallback?.Invoke(framework);
        uiInteraction.Received(1).SelectRetainer("MyRetainer");

        service.MockNow = service.MockNow.AddSeconds(1);

        // Tick 4: Retainer is summoned (SelectString appears for the targeted retainer)
        uiInteraction.IsAddonReady("RetainerList").Returns(false);
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        updateCallback?.Invoke(framework);

        // Final validation: Market menu should be forcefully opened
        uiInteraction.Received(1).OpenRetainerMarket();
    }
}