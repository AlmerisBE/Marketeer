using Marketeer.Core.MarketListings.Models;
using Marketeer.Core.RetainerAutomation.Models;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IListingActionResolverService {
    void ProcessListingClick(TrackedListing listing);
    ListingClickAction ResolveAction(uint itemId);
}