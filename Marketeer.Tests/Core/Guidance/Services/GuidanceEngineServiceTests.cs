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
    public void EvaluateState_PrioritizesMostExpensiveUndercut() {
        var mockGameEvent = Substitute.For<IGameEventService>();
        var mockListing = Substitute.For<IMarketListingProvider>();
        var mockRetainer = Substitute.For<IRetainerProvider>();
        var mockCompetition = Substitute.For<ICompetitionStateService>();
        var mockOptimization = Substitute.For<IListingOptimizationService>();

        mockListing.GetActiveRetainerId().Returns(123ul);
        mockRetainer.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 123ul, Name = "TestRetainer" }
        });

        // Simule deux undercuts : un à 5000 et un à 10000
        mockCompetition.GetUndercutItems().Returns(new List<UndercutItem> {
            new UndercutItem { RetainerName = "TestRetainer", ItemName = "Cheap Item", OurPrice = 5000, ServerCheapestPrice = 4500 },
            new UndercutItem { RetainerName = "TestRetainer", ItemName = "Expensive Item", OurPrice = 10000, ServerCheapestPrice = 9000 }
        });

        var service = new GuidanceEngineService(mockGameEvent, mockListing, mockRetainer, mockCompetition, mockOptimization);

        // Act
        // On déclenche manuellement l'évaluation via reflection pour le test, ou on invoque l'event
        mockGameEvent.RetainerSellListUpdated += Raise.Event<System.Action>();

        var instruction = service.GetCurrentInstruction();

        // Assert
        Assert.NotNull(instruction);
        Assert.Equal(GuidanceActionType.UpdatePrice, instruction.ActionType);
        Assert.Equal("Expensive Item", instruction.ItemName);
        Assert.Equal(8999u, instruction.TargetPrice);
    }
}