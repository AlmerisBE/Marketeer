using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.Core.RetainerAutomation.Components;

public class RetainerScanSidebarAction : ISidebarAction {
    private IRetainerAutomationService automationService;
    private ILocalizationService localizationService;

    public string Name => this.localizationService.Translate("Dashboard_ScanRetainers") ?? "Scan Retainers";
    public int Priority => 100;

    public RetainerScanSidebarAction(IRetainerAutomationService automationService, ILocalizationService localizationService) {
        this.automationService = automationService;
        this.localizationService = localizationService;
    }

    public void Execute() {
        this.automationService.TriggerScan();
    }
}