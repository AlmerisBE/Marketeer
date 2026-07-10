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
        var mockCommandManager = Substitute.For<ICommandManager>();
        var mockClientState = Substitute.For<IClientState>();
        var mockLogger = Substitute.For<IPluginLog>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        mockObjectTable.LocalPlayer.Returns((IPlayerCharacter?)null);

        // Act & Assert
        // We verify that building the plugin (and its DI container) throws no exceptions
        var exception = Record.Exception(() => new Plugin(
            mockPluginInterface,
            mockChatGui,
            mockCommandManager,
            mockClientState,
            mockLogger,
            mockObjectTable));

        Assert.Null(exception);
    }
}