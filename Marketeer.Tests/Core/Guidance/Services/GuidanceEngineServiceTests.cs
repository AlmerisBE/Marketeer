using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.Core.Guidance.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Guidance.Services;

public class GuidanceEngineServiceTests {

    [Fact]
    public void GetCurrentInstruction_PrioritizesMostExpensiveUndercutForCurrentRetainer() {
        var mockListing = Substitute.For<IMarketListingProvider>();
        var mockRetainer = Substitute.For<IRetainerProvider>();
        var mockCompetition = Substitute.For<ICompetitionStateService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();

        mockListing.GetActiveRetainerId().Returns(123ul);
        mockRetainer.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 123ul, Name = "TestRetainer" }
        });

        mockCompetition.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { RetainerName = "TestRetainer", ItemName = "Cheap Item", OurPrice = 5000, ServerCheapestPrice = 4500 },
            new UndercutItem { RetainerName = "TestRetainer", ItemName = "Expensive Item", OurPrice = 10000, ServerCheapestPrice = 9000 }
        });

        var service = new GuidanceEngineService(mockListing, mockRetainer, mockCompetition, mockOptimization);
        var instruction = service.GetCurrentInstruction();

        Assert.NotNull(instruction);
        Assert.Equal(GuidanceActionType.UpdatePrice, instruction.ActionType);
        Assert.Equal("Expensive Item", instruction.ItemName);
        Assert.Equal(8999u, instruction.TargetPrice);
    }

    [Fact]
    public void GetCurrentInstruction_SuggestsSwitchRetainer_WhenCurrentRetainerIsOptimized() {
        var mockListing = Substitute.For<IMarketListingProvider>();
        var mockRetainer = Substitute.For<IRetainerProvider>();
        var mockCompetition = Substitute.For<ICompetitionStateService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();

        mockListing.GetActiveRetainerId().Returns(123ul); // Retainer 1 is active
        mockRetainer.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 123ul, Name = "Retainer1" },
            new TrackedRetainer { RetainerId = 456ul, Name = "Retainer2" }
        });

        // Undercut exists, but for Retainer2!
        mockCompetition.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { RetainerName = "Retainer2", ItemName = "Other Item", OurPrice = 10000, ServerCheapestPrice = 9000 }
        });

        var service = new GuidanceEngineService(mockListing, mockRetainer, mockCompetition, mockOptimization);
        var instruction = service.GetCurrentInstruction();

        Assert.NotNull(instruction);
        Assert.Equal(GuidanceActionType.SwitchRetainer, instruction.ActionType);
        Assert.Equal("Retainer2", instruction.RetainerName);
    }
}