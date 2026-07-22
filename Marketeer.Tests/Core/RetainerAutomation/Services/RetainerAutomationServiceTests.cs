using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
using Marketeer.Core.RetainerAutomation.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class RetainerAutomationServiceTests {
    [Fact]
    public void TriggerScan_WhenRetainersExist_StartsOrchestration() {
        var mockOrchestrator = Substitute.For<IRetainerOrchestratorService>();
        var mockRetainerProvider = Substitute.For<IRetainerProvider>();
        var mockMarketTracker = Substitute.For<IMarketListingTrackerService>();
        var mockLogger = Substitute.For<ILoggerService>();

        var retainers = new List<TrackedRetainer> {
            new TrackedRetainer { Name = "AlmerisRetainer", RetainerId = 1001 }
        };
        mockRetainerProvider.GetActiveRetainers().Returns(retainers);

        var service = new RetainerAutomationService(
            mockOrchestrator, mockRetainerProvider, mockMarketTracker, mockLogger);

        service.TriggerScan();

        mockOrchestrator.Received(1).StartOrchestration(
            Arg.Is<IEnumerable<string>>(list => list.Contains("AlmerisRetainer")),
            RetainerTargetMenu.MarketListings,
            service);
    }
}