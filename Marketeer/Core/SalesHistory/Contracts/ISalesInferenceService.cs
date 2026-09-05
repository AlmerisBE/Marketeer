using Marketeer.Core.SalesHistory.Models;
using System.Collections.Generic;

namespace Marketeer.Core.SalesHistory.Contracts;

public interface ISalesInferenceService {
    void InferSales(
        ulong retainerId,
        bool isFirstScan,
        IReadOnlyList<ListingState> previousListings,
        IReadOnlyList<ListingState> currentListings);
}