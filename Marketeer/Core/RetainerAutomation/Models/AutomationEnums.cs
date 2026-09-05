namespace Marketeer.Core.RetainerAutomation.Models;

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
    WaitSelectStringOpen,
    OpenMenu,
    WaitMenu,
    ExecutingTask,
    CloseMenu,
    WaitMenuClosed,
    WaitSelectStringReturn,
    CloseSelectString,
    WaitRetainerListReturn,
    CloseRetainerList
}