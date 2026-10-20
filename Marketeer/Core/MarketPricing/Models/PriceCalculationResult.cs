namespace Marketeer.Core.MarketPricing.Models;

public enum PricingAction {
    DecreasePrice,
    IncreasePrice,
    KeepPrice,
    CancelListing
}

public class PriceCalculationResult {
    public PricingAction Action { get; set; }
    public uint CalculatedPrice { get; set; }
}