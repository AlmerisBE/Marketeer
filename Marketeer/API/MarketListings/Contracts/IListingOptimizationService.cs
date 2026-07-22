using Marketeer.API.MarketListings.Models;
using System.Collections.Generic;

namespace Marketeer.API.MarketListings.Contracts;

public interface IListingOptimizationService {
    IReadOnlyList<SuboptimalListing> GetVendorPricedListings();
}