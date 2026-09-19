using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;

namespace Marketeer.UI.RetainerAutomation.Components;

public class ScanRetainersToolbarAction : IToolbarAction {
    private IRetainerAutomationService automationService;
    private ILocalizationService localization;

    public string Name => this.localization.Translate("Toolbar_ScanRetainers");
    public string Tooltip => this.localization.Translate("Toolbar_ScanRetainers_Tooltip");
    public int Priority => 10;
    public bool IsEnabled => !this.automationService.IsScanning;

    public ScanRetainersToolbarAction(IRetainerAutomationService automationService, ILocalizationService localization) {
        this.automationService = automationService;
        this.localization = localization;
    }

    public void Execute() {
        this.automationService.TriggerScan();
    }
}