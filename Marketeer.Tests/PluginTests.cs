using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests;

public class PluginTests {
    [Fact]
    public void Plugin_Constructor_ShouldInitializeSuccessfully() {
        var mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockGameGui = Substitute.For<IGameGui>();
        var mockCommandManager = Substitute.For<ICommandManager>();
        var mockClientState = Substitute.For<IClientState>();
        var mockPluginLog = Substitute.For<IPluginLog>();
        var mockObjectTable = Substitute.For<IObjectTable>();
        var mockFramework = Substitute.For<IFramework>();
        var mockDataManager = Substitute.For<IDataManager>();
        var mockCondition = Substitute.For<ICondition>();
        var mockAddonLifecycle = Substitute.For<IAddonLifecycle>();
        var mockTextureProvider = Substitute.For<ITextureProvider>();

        var mockContextMenu = Substitute.For<IContextMenu>(); // Mock de IContextMenu

        var exception = Record.Exception(() => new Plugin(
            mockPluginInterface,
            mockChatGui,
            mockGameGui,
            mockCommandManager,
            mockClientState,
            mockPluginLog,
            mockObjectTable,
            mockFramework,
            mockDataManager,
            mockCondition,
            mockAddonLifecycle,
            mockTextureProvider,
            mockContextMenu // Injection du mock manquant
        ));

        Assert.Null(exception);
    }
}