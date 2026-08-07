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
    OpenBell,
    WaitBell,
    SelectRetainer,
    OpenMenu,
    WaitMenu,
    ExecutingTask,
    CloseMenu,
    WaitMenuClosed,
    CloseSelectString,
    CloseRetainerList
}