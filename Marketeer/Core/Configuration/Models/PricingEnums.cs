namespace Marketeer.Core.Configuration.Models;

public enum UndercutMode {
    Absolute,
    Relative
}

public enum WhitelistBehavior {
    MatchPrice,
    Ignore
}

public enum MinimumPriceBehavior {
    SellAnyway,
    KeepCurrentPrice,
    CancelToInventory
}

public enum InventoryPriority {
    RetainerFirst,
    PlayerFirst
}

public enum FallbackPricingMode {
    AverageListingPrice,
    MaxListingPrice,
    VendorSellMultiple,
    VendorBuyMultiple
}

public enum AnomalyDefenseStrategy {
    Ignore,
    HoldPrice,
    AlertAndPause
}