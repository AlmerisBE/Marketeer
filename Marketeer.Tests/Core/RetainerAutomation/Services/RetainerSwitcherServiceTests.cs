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
    public void SwitchTo_HandlesSelectYesNo_AndContinuesSwitchProcess() {
        var framework = Substitute.For<IFramework>();
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var logger = Substitute.For<ILoggerService>();

        uiInteraction.CloseRetainerMarket().Returns(true);
        uiInteraction.CloseSalesHistory().Returns(true);
        uiInteraction.CloseSelectString().Returns(true);
        uiInteraction.SelectRetainer(Arg.Any<string>()).Returns(true);

        IFramework.OnUpdateDelegate? updateCallback = null;
        framework.When(x => x.Update += Arg.Any<IFramework.OnUpdateDelegate>())
            .Do(x => updateCallback = x.Arg<IFramework.OnUpdateDelegate>());

        using var service = new TestableRetainerSwitcherService(framework, uiInteraction, logger);

        service.SwitchTo("MyRetainer", openMarketList: false);

        // Simulate the appearance of the buyback confirmation dialog
        uiInteraction.IsAddonReady("SelectYesNo").Returns(true);
        updateCallback?.Invoke(framework);

        uiInteraction.Received(1).ConfirmYesNo();

        service.MockNow = service.MockNow.AddSeconds(1);

        // The dialog is now closed, process should resume normally to SelectString
        uiInteraction.IsAddonReady("SelectYesNo").Returns(false);
        uiInteraction.IsAddonReady("SelectString").Returns(true);
        updateCallback?.Invoke(framework);

        uiInteraction.Received(1).CloseSelectString();
    }
}