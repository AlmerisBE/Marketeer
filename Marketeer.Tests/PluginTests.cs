using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests;

public class PluginTests {

    [Fact]
    public void Plugin_OnInitialization_BuildsDependencyInjectionWithoutErrors() {
        // Arrange
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockGameUi = Substitute.For<IGameGui>();
        var mockCommandManager = Substitute.For<ICommandManager>();
        var mockClientState = Substitute.For<IClientState>();
        var mockLogger = Substitute.For<IPluginLog>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockFramework = Substitute.For<IFramework>();
        var mockDataManager = Substitute.For<IDataManager>();
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();

        mockObjectTable.LocalPlayer.Returns((IPlayerCharacter?)null);

        // Act & Assert
        var exception = Record.Exception(() => new Plugin(
            mockPluginInterface,
            mockChatGui,
            mockGameUi,
            mockCommandManager,
            mockClientState,
            mockLogger,
            mockObjectTable,
            mockFramework,
            mockDataManager,
            mockCondition,
            mockAddonLifecycle));

        Assert.Null(exception);
    }
}