namespace Marketeer.API.RetainerAutomation.Services;

public enum AutomationStep {
    Idle,
    SelectRetainer,
    OpenMarketListings,
    ScanMarketListings,
    WaitAndCloseMarketListings,
    CloseSelectString,
    CloseRetainerList
}
public enum OrchestrationStep {
    Idle,
    SelectRetainer,
    OpenMenu,
    WaitMenu,
    ExecutingTask,
    CloseMenu,
    WaitMenuClosed,
    CloseSelectString,
    CloseRetainerList
}