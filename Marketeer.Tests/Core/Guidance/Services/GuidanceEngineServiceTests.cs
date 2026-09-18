using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Models;
using Marketeer.UI.RetainerOverlays.Models;
using Marketeer.UI.RetainerOverlays.Services;
using Marketeer.UI.UiInterop.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.Guidance.Services;

public class GuidanceEngineServiceTests {
    private IMarketListingProvider listingProvider;
    private IRetainerProvider retainerProvider;
    private ICompetitionStateService competitionState;
    private IListingOptimizationService optimizationService;
    private IObjectTable objectTable;
    private INativeWindowService windowService;
    private IPlayerCharacter localPlayer;
    private GuidanceEngineService service;

    public GuidanceEngineServiceTests() {
        this.listingProvider = Substitute.For<IMarketListingProvider>();
        this.retainerProvider = Substitute.For<IRetainerProvider>();
        this.competitionState = Substitute.For<ICompetitionStateService>();
        this.optimizationService = Substitute.For<IListingOptimizationService>();
        this.objectTable = Substitute.For<IObjectTable>();
        this.windowService = Substitute.For<INativeWindowService>();
        this.localPlayer = Substitute.For<IPlayerCharacter>();

        var seName = new SeString(new TextPayload("Player One"));
        this.localPlayer.Name.Returns(seName);
        this.objectTable.LocalPlayer.Returns(this.localPlayer);

        this.service = new GuidanceEngineService(
            this.listingProvider,
            this.retainerProvider,
            this.competitionState,
            this.optimizationService,
            this.objectTable,
            this.windowService);
    }

    [Fact]
    public void GetCurrentInstruction_SuggestsSummon_WhenAtRetainerList() {
        this.listingProvider.GetActiveRetainerId().Returns(1ul);

        this.retainerProvider.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 1ul, Name = "MyRetainer" }
        }.AsReadOnly());

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                CharacterName = "Player One",
                RetainerName = "MyRetainer",
                ItemName = "Cheap Item",
                OurPrice = 1000,
                TargetPrice = 899
            }
        };

        this.competitionState.GetUndercutItems().Returns(undercuts.AsReadOnly());
        this.optimizationService.GetVendorPricedListings().Returns(new List<SuboptimalListing>().AsReadOnly());

        var mockWindow = Substitute.For<INativeWindow>();
        mockWindow.IsVisible.Returns(true);
        this.windowService.GetWindow("RetainerList").Returns(mockWindow);

        var result = this.service.GetCurrentInstruction();

        Assert.NotNull(result);
        Assert.Equal(GuidanceActionType.SummonRetainer, result.ActionType);
        Assert.Equal("MyRetainer", result.RetainerName);
    }

    [Fact]
    public void GetCurrentInstruction_PrioritizesMostExpensiveUndercutForCurrentRetainer() {
        this.listingProvider.GetActiveRetainerId().Returns(1ul);

        this.retainerProvider.GetActiveRetainers().Returns(new List<TrackedRetainer> {
            new TrackedRetainer { RetainerId = 1ul, Name = "MyRetainer" }
        }.AsReadOnly());

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                CharacterName = "Player One",
                RetainerName = "MyRetainer",
                ItemName = "Cheap Item",
                OurPrice = 1000,
                ServerCheapestPrice = 900,
                TargetPrice = 899
            },
            new UndercutItem {
                CharacterName = "Player One",
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
                CharacterName = "Player One",
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
                CharacterName = "Player One",
                RetainerName = "MyRetainer",
                ItemName = "Vendor Trash",
                Price = 5,
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

    [Fact]
    public void GetCurrentInstruction_SuggestsSummon_WhenNoRetainerActiveAndHasUndercuts() {
        this.listingProvider.GetActiveRetainerId().Returns((ulong?)null);

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                CharacterName = "Player One",
                RetainerName = "MyRetainer",
                ItemName = "Some Item",
                OurPrice = 5000,
                TargetPrice = 4999
            }
        };

        this.competitionState.GetUndercutItems().Returns(undercuts.AsReadOnly());
        this.optimizationService.GetVendorPricedListings().Returns(new List<SuboptimalListing>().AsReadOnly());

        var result = this.service.GetCurrentInstruction();

        Assert.NotNull(result);
        Assert.Equal(GuidanceActionType.SummonRetainer, result.ActionType);
        Assert.Equal("MyRetainer", result.RetainerName);
    }

    [Fact]
    public void GetCurrentInstruction_SuggestsSwitchCharacter_WhenUndercutsAreOnOtherCharacter() {
        this.listingProvider.GetActiveRetainerId().Returns((ulong?)null);

        var undercuts = new List<UndercutItem> {
            new UndercutItem {
                CharacterName = "Player Two",
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
        Assert.Equal(GuidanceActionType.SwitchCharacter, result.ActionType);
        Assert.Equal("Player Two", result.CharacterName);
    }
}