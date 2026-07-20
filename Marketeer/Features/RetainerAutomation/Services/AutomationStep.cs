namespace Marketeer.Features.RetainerAutomation.Services;

public enum AutomationStep {
    Idle,
    SelectRetainer,
    OpenMarketListings,
    ScanMarketListings,
    WaitAndCloseMarketListings,
    CloseSelectString,
    CloseRetainerList
}