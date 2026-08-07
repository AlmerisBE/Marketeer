using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.Core.Guidance.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Guidance.Services;

public class GuidanceEngineServiceTests {
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private GuidanceEngineService service;

    public GuidanceEngineServiceTests() {
        this.listingProvider = Substitute.For<IMarketListingProvider>();
        this.retainerProvider = Substitute.For<IRetainerProvider>();
        this.competitionState = Substitute.For<ICompetitionStateService>();
        this.optimizationService = Substitute.For<IListingOptimizationService>();

        this.service = new GuidanceEngineService(
            this.listingProvider,
            this.retainerProvider,
            this.competitionState,
            this.optimizationService);
    }

    [Fact]
    public void GetCurrentInstruction_PrioritizesMostExpensiveUndercutForCurrentRetainer() {
        this.listingProvider.GetActiveRetainerId().Returns(1ul);

        this.retainerProvider.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 1ul, Name = "MyRetainer" }
        }.AsReadOnly());

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                RetainerName = "MyRetainer",
                ItemName = "Cheap Item",
                OurPrice = 1000,
                ServerCheapestPrice = 900,
                TargetPrice = 899
            },
            new UndercutItem {
                RetainerName = "MyRetainer",
                ItemName = "Expensive Item",
                OurPrice = 9000,
                ServerCheapestPrice = 9000,
                TargetPrice = 8999
            }
        };

        this.competitionState.GetUndercutItems().Returns(undercuts.AsReadOnly());
        this.optimizationService.GetVendorPricedListings().Returns(new List<SuboptimalListing>().AsReadOnly());

        var result = this.service.GetCurrentInstruction();

        Assert.NotNull(result);
        Assert.Equal(GuidanceActionType.UpdatePrice, result.ActionType);
        Assert.Equal("Expensive Item", result.ItemName);
        Assert.Equal(8999u, result.TargetPrice);
    }

    [Fact]
    public void GetCurrentInstruction_SuggestsSwitch_WhenCurrentRetainerIsOptimized() {
        this.listingProvider.GetActiveRetainerId().Returns(1ul);

        this.retainerProvider.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 1ul, Name = "OptimizedRetainer" }
        }.AsReadOnly());

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                RetainerName = "OtherRetainer",
                ItemName = "Some Item",
                OurPrice = 5000,
                TargetPrice = 4999
            }
        };

        this.competitionState.GetUndercutItems().Returns(undercuts.AsReadOnly());
        this.optimizationService.GetVendorPricedListings().Returns(new List<SuboptimalListing>().AsReadOnly());

        var result = this.service.GetCurrentInstruction();

        Assert.NotNull(result);
        Assert.Equal(GuidanceActionType.SwitchRetainer, result.ActionType);
        Assert.Equal("OtherRetainer", result.RetainerName);
    }

    [Fact]
    public void GetCurrentInstruction_SuggestsSuboptimalCancel_WhenCurrentRetainerHasNoUndercuts() {
        this.listingProvider.GetActiveRetainerId().Returns(1ul);

        this.retainerProvider.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 1ul, Name = "MyRetainer" }
        }.AsReadOnly());

        this.competitionState.GetUndercutItems().Returns(new List<UndercutItem>().AsReadOnly());

        var suboptimalListings = new List<SuboptimalListing> {
            new SuboptimalListing {
                RetainerName = "MyRetainer",
                ItemName = "Vendor Trash",
                CurrentPrice = 5,
                VendorPrice = 10
            }
        };

        this.optimizationService.GetVendorPricedListings().Returns(suboptimalListings.AsReadOnly());

        var result = this.service.GetCurrentInstruction();

        Assert.NotNull(result);
        Assert.Equal(GuidanceActionType.CancelListing, result.ActionType);
        Assert.Equal("Vendor Trash", result.ItemName);
        Assert.Equal("MyRetainer", result.RetainerName);
    }
}