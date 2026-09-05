using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.Core.SalesHistory.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.SalesHistory.Services;

public class SalesInferenceServiceTests {
    [Fact]
    public void InferSales_WhenFirstScanAndSlotEmpties_RecordsSale() {
        var salesRepository = Substitute.For<ISalesRepository>();
        var logger = Substitute.For<ILoggerService>();

        var service = new SalesInferenceService(salesRepository, logger);

        var previousListings = new List<ListingState> {
            new ListingState { SlotIndex = 0, ItemId = 100, Quantity = 1, UnitPrice = 5000, ListingDate = DateTime.Now.AddDays(-1) }
        };
        var currentListings = new List<ListingState>(); // Slot is now empty

        service.InferSales(12345, true, previousListings, currentListings);

        // Correction : Utilisation de LINQ .Any() au lieu de manipuler manuellement l'IEnumerator
        salesRepository.Received(1).AddSales(Arg.Is<IEnumerable<SaleRecord>>(records =>
            records.Any(r => r.ItemId == 100 && r.UnitPrice == 5000)
        ));

        logger.Received(1).Debug(Arg.Is<string>(s => s.Contains("Inferred sale")));
    }

    [Fact]
    public void InferSales_WhenSubsequentScanAndSlotEmpties_LogsCancellationAndDoesNotRecordSale() {
        var salesRepository = Substitute.For<ISalesRepository>();
        var logger = Substitute.For<ILoggerService>();

        var service = new SalesInferenceService(salesRepository, logger);

        var previousListings = new List<ListingState> {
            new ListingState { SlotIndex = 0, ItemId = 100, Quantity = 1, UnitPrice = 5000, ListingDate = DateTime.Now.AddDays(-1) }
        };
        var currentListings = new List<ListingState>(); // Slot is now empty

        service.InferSales(12345, false, previousListings, currentListings);

        salesRepository.DidNotReceive().AddSales(Arg.Any<IEnumerable<SaleRecord>>());

        logger.Received(1).Debug(Arg.Is<string>(s => s.Contains("Manual cancellation detected")));
    }

    [Fact]
    public void InferSales_WhenSlotRemainsOccupied_DoesNotRecordSale() {
        var salesRepository = Substitute.For<ISalesRepository>();
        var logger = Substitute.For<ILoggerService>();

        var service = new SalesInferenceService(salesRepository, logger);

        var previousListings = new List<ListingState> {
            new ListingState { SlotIndex = 0, ItemId = 100, Quantity = 1, UnitPrice = 5000, ListingDate = DateTime.Now.AddDays(-1) }
        };
        var currentListings = new List<ListingState> {
            new ListingState { SlotIndex = 0, ItemId = 100, Quantity = 1, UnitPrice = 5000 }
        };

        service.InferSales(12345, true, previousListings, currentListings);

        salesRepository.DidNotReceive().AddSales(Arg.Any<IEnumerable<SaleRecord>>());
    }
}