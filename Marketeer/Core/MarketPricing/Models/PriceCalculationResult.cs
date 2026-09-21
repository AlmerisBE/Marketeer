namespace Marketeer.Core.MarketPricing.Models;

public enum PricingAction {
    UpdatePrice,
    KeepPrice,
    CancelListing
}

public class PriceCalculationResult {
    public PricingAction Action { get; set; }
    public uint CalculatedPrice { get; set; }
}