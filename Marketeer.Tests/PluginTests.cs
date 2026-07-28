using Dalamud.Interface;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests;

public class PluginTests {
    [Fact]
    public void Plugin_Initialization_Succeeds() {
        // Arrange
        var pluginInterface = Substitute.For<IDalamudPluginInterface>();
        var chatGui = Substitute.For<IChatGui>();
        var gameGui = Substitute.For<IGameGui>();
        var commandManager = Substitute.For<ICommandManager>();
        var clientState = Substitute.For<IClientState>();
        var pluginLog = Substitute.For<IPluginLog>();
        var objectTable = Substitute.For<IObjectTable>();
        var framework = Substitute.For<IFramework>();
        var dataManager = Substitute.For<IDataManager>();
        var condition = Substitute.For<ICondition>();
        var addonLifecycle = Substitute.For<IAddonLifecycle>();
        var textureProvider = Substitute.For<ITextureProvider>();

        // We mock the UiBuilder to avoid NullReferenceExceptions when Plugin hooks events
        var uiBuilder = Substitute.For<UiBuilder>();
        pluginInterface.UiBuilder.Returns(uiBuilder);

        // Act
        var plugin = new Plugin(
            pluginInterface,
            chatGui,
            gameGui,
            commandManager,
            clientState,
            pluginLog,
            objectTable,
            framework,
            dataManager,
            condition,
            addonLifecycle,
            textureProvider
        );

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("Marketeer", plugin.Name);
    }
}