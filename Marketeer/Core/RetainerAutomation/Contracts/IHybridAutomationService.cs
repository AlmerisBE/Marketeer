using Marketeer.Core.MarketListings.Models;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IHybridAutomationService {
    bool IsActive { get; }
    void StartPriceUpdate(TrackedListing listing, nint addonAddress);
}