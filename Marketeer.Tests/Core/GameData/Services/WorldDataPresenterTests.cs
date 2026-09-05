using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Marketeer.API.GameData.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.GameData.Services;

public class WorldDataPresenterTests {
    [Fact]
    public void GetWorldName_WhenSheetIsMissing_ReturnsIdAsString() {
        // Arrange
        var mockDataManager = Substitute.For<IDataManager>();

        // Simulate Dalamud failing to load the Excel sheet
        mockDataManager.GetExcelSheet<World>().Returns((ExcelSheet<World>?)null);

        var service = new WorldDataPresenter(mockDataManager);
        uint testId = 33;

        // Act
        var result = service.GetWorldName(testId);

        // Assert
        Assert.Equal("33", result);
    }
}