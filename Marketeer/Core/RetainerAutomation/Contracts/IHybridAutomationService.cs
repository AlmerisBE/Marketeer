using Marketeer.Core.MarketListings.Models;
using System;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IHybridAutomationService {
    event Action<uint>? CancellationRequested;
    bool IsActive { get; }
    void StartPriceUpdate(TrackedListing listing);
    void StartNewSale(uint itemId, string itemName);
}