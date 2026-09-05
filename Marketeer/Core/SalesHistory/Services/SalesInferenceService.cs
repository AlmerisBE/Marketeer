using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.SalesHistory.Services;

public class SalesInferenceService : ISalesInferenceService {
    private ISalesRepository salesRepository;
    private ILoggerService logger;

    public SalesInferenceService(ISalesRepository salesRepository, ILoggerService logger) {
        this.salesRepository = salesRepository;
        this.logger = logger;
    }

    public void InferSales(
        ulong retainerId,
        bool isFirstScan,
        IReadOnlyList<ListingState> previousListings,
        IReadOnlyList<ListingState> currentListings) {

        var inferredSales = new List<SaleRecord>();

        // Step 2 & 8: Loop through the previous listings to detect disappearances
        foreach (var previousListing in previousListings) {
            var currentListing = currentListings.FirstOrDefault(c => c.SlotIndex == previousListing.SlotIndex);

            // Step 6 & 9: If the slot is currently occupied, no sale or cancellation happened.
            // Note: The caller service is responsible for updating the saved characteristics of this listing.
            if (currentListing != null) {
                continue;
            }

            if (isFirstScan) {
                this.logger.Debug($"Inferred sale for Item ID {previousListing.ItemId} on Slot {previousListing.SlotIndex}. Price: {previousListing.UnitPrice}.");

                inferredSales.Add(new SaleRecord {
                    RetainerId = retainerId,
                    ItemId = previousListing.ItemId,
                    Quantity = previousListing.Quantity,
                    UnitPrice = previousListing.UnitPrice,
                    BuyerName = "Unknown (Inferred)",
                    SaleDate = DateTime.UtcNow,
                    ListingDate = previousListing.ListingDate
                });
            }
            else {
                // Step 11: Subsequent scan. The retainer is currently summoned, so a disappearance is a manual cancellation.
                this.logger.Debug($"Manual cancellation detected for Item ID {previousListing.ItemId} on Slot {previousListing.SlotIndex}. No sale inferred.");
            }
        }

        // Step 5 & 12: Empty slots that remained empty are naturally ignored by the loop over previousListings.

        if (inferredSales.Count > 0) {
            this.salesRepository.AddSales(inferredSales);
        }
    }
}