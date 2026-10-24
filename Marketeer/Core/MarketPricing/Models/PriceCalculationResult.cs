namespace Marketeer.Core.MarketPricing.Models;

public enum PricingAction {
    DecreasePrice,
    IncreasePrice,
    KeepPrice,
    CancelListing
}

public enum PricingReason {
    Unknown,
    StorageProtection,
    MarketCrashProtection,
    NoCompetition,
    VendorMinimumReached,
    MatchedWhitelistedCompetitor,
    UndercutCompetitor,
    AlreadyLowest
}

public class PriceCalculationResult {
    public PricingAction Action { get; set; }
    public uint CalculatedPrice { get; set; }
    public PricingReason Reason { get; set; } = PricingReason.Unknown;
    public string ContextData { get; set; } = string.Empty;
    public uint CompetitorPrice { get; set; }
}