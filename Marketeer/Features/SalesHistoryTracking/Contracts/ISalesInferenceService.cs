using Marketeer.Features.SalesHistoryTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.SalesHistoryTracking.Contracts;

public interface ISalesInferenceService {
    void InferSales(
        ulong retainerId,
        bool isFirstScan,
        IReadOnlyList<ListingState> previousListings,
        IReadOnlyList<ListingState> currentListings);
}