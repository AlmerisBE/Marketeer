using Marketeer.Core.MarketListings.Models;
using System.Collections.Generic;

namespace Marketeer.Core.MarketListings.Contracts;

public interface IListingOptimizationService {
    IReadOnlyList<SuboptimalListing> GetVendorPricedListings();
}