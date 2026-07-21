using Marketeer.API.SalesHistory.Models;
using Marketeer.UI.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.SalesHistoryUI.Services;

public class SalesDataPresenterTests {
    private List<ISalesViewRecord> GetTestData() {
        var potion = Substitute.For<ISalesViewRecord>();
        potion.Name.Returns("Potion");
        potion.TotalQuantitySold.Returns(100u);
        potion.TotalRevenue.Returns(1000ul);
        potion.LastSaleDate.Returns(new DateTime(2023, 1, 1));

        var hiPotion = Substitute.For<ISalesViewRecord>();
        hiPotion.Name.Returns("Hi-Potion");
        hiPotion.TotalQuantitySold.Returns(50u);
        hiPotion.TotalRevenue.Returns(2000ul);
        hiPotion.LastSaleDate.Returns(new DateTime(2023, 1, 3));

        var ether = Substitute.For<ISalesViewRecord>();
        ether.Name.Returns("Ether");
        ether.TotalQuantitySold.Returns(200u);
        ether.TotalRevenue.Returns(1500ul);
        ether.LastSaleDate.Returns(new DateTime(2023, 1, 2));

        return new List<ISalesViewRecord> { potion, hiPotion, ether };
    }

    [Fact]
    public void ProcessData_WithSearchQuery_FiltersByNameCaseInsensitive() {
        var presenter = new SalesDataPresenter();
        var data = this.GetTestData();

        var result = presenter.ProcessData(data, "potion", SalesSortColumn.None, true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Name == "Potion");
        Assert.Contains(result, x => x.Name == "Hi-Potion");
    }

    [Fact]
    public void ProcessData_SortByQuantityDescending_ReturnsCorrectOrder() {
        var presenter = new SalesDataPresenter();
        var data = this.GetTestData();

        var result = presenter.ProcessData(data, "", SalesSortColumn.Quantity, false);

        Assert.Equal(3, result.Count);
        Assert.Equal("Ether", result[0].Name);
        Assert.Equal("Potion", result[1].Name);
        Assert.Equal("Hi-Potion", result[2].Name);
    }
}