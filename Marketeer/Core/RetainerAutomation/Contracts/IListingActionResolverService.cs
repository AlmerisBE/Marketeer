using Marketeer.Core.RetainerAutomation.Models;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IListingActionResolverService {
    ListingClickAction ResolveAction(uint itemId);
}