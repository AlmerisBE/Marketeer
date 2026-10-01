using Marketeer.Core.MarketPricing.Models;

namespace Marketeer.UI.CompetitionTracking.Models;

public class UndercutItem {
    public int SlotIndex { get; set; }
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public uint Price { get; set; }
    public uint OurPrice { get; set; }
    public uint ServerCheapestPrice { get; set; }
    public uint TargetPrice { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public PricingAction SuggestedAction { get; set; } = PricingAction.UpdatePrice;
}