using Marketeer.API.Logging.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using Marketeer.API.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Features.SalesHistoryTracking.Services;

public class SalesInferenceServiceTests {

    [Fact]
    public void InferSales_WhenFirstScanAndSlotEmpties_RecordsSale() {
        // Arrange
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockLogger = Substitute.For<ILoggerService>();

        var prevListings = new List<ListingState> { new ListingState { SlotIndex = 0, ItemId = 1001, Quantity = 2, UnitPrice = 5000 } };
        var currListings = new List<ListingState>();

        var service = new SalesInferenceService(mockRepository, mockLogger);

        // Act
        service.InferSales(1, isFirstScan: true, prevListings, currListings);

        // Assert
        mockRepository.Received(1).AddSales(Arg.Is<IEnumerable<SaleRecord>>(records =>
            records.Count() == 1 && records.First().ItemId == 1001));
        mockLogger.Received(1).Info(Arg.Is<string>(s => s.Contains("Inferred sale")));
    }

    [Fact]
    public void InferSales_WhenSubsequentScanAndSlotEmpties_LogsCancellationAndDoesNotRecordSale() {
        // Arrange
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockLogger = Substitute.For<ILoggerService>();

        var prevListings = new List<ListingState> { new ListingState { SlotIndex = 0, ItemId = 1001, Quantity = 2, UnitPrice = 5000 } };
        var currListings = new List<ListingState>();

        var service = new SalesInferenceService(mockRepository, mockLogger);

        // Act
        service.InferSales(1, isFirstScan: false, prevListings, currListings);

        // Assert
        mockRepository.DidNotReceiveWithAnyArgs().AddSales(default!);
        mockLogger.Received(1).Info(Arg.Is<string>(s => s.Contains("Manual cancellation detected")));
    }

    [Fact]
    public void InferSales_WhenSlotRemainsOccupied_DoesNothingRegardlessOfScanType() {
        // Arrange
        var mockRepository = Substitute.For<ISalesRepository>();
        var mockLogger = Substitute.For<ILoggerService>();

        var prevListings = new List<ListingState> { new ListingState { SlotIndex = 2, ItemId = 1005, Quantity = 1, UnitPrice = 1000 } };
        var currListings = new List<ListingState> { new ListingState { SlotIndex = 2, ItemId = 1005, Quantity = 1, UnitPrice = 1000 } };

        var service = new SalesInferenceService(mockRepository, mockLogger);

        // Act
        service.InferSales(1, isFirstScan: true, prevListings, currListings);
        service.InferSales(1, isFirstScan: false, prevListings, currListings);

        // Assert
        mockRepository.DidNotReceiveWithAnyArgs().AddSales(default!);
    }
}