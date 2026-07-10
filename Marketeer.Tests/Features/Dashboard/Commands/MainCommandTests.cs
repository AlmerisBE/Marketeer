using Marketeer.Features.Dashboard.Commands;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.UI;
using Xunit;

namespace Marketeer.Tests.Features.Dashboard.Commands;

public class MainCommandTests {
    [Fact]
    public void MainCommand_Execute_TogglesWindowVisibility() {
        // Arrange
        var emptyWidgets = new List<IDashboardWidget>();
        var window = new DashboardWindow(emptyWidgets);
        var command = new MainCommand(window);

        window.IsOpen = false;

        // Act 1: Execute when closed
        command.Execute(string.Empty);

        // Assert 1: Window should now be open
        Assert.True(window.IsOpen);

        // Act 2: Execute when open
        command.Execute(string.Empty);

        // Assert 2: Window should now be closed
        Assert.False(window.IsOpen);
    }
}