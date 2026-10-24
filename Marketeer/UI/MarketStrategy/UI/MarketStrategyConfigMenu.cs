using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.Components;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.MarketStrategy.UI;

public class MarketStrategyConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    private MarketAnomalySettingsComponent anomalyComponent;
    private StorageAndOutlierSettingsComponent protectionComponent;
    private GlobalPricingSettingsComponent globalPricingComponent;
    private PricingProfileManagementComponent profileManagementComponent;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Menu_MarketStrategy") ?? "Market Strategy";
    public int Priority => 101;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public MarketStrategyConfigMenu(IConfigurationService configService, ILocalizationService localizationService, IDataManager dataManager) {
        this.configService = configService;
        this.localizationService = localizationService;

        this.anomalyComponent = new MarketAnomalySettingsComponent(localizationService);
        this.protectionComponent = new StorageAndOutlierSettingsComponent(localizationService);
        this.globalPricingComponent = new GlobalPricingSettingsComponent(localizationService);
        this.profileManagementComponent = new PricingProfileManagementComponent(localizationService, dataManager);
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        if (this.anomalyComponent.Draw(config)) changed = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.protectionComponent.Draw(config)) changed = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.globalPricingComponent.Draw(config)) changed = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.profileManagementComponent.Draw(config)) changed = true;

        if (changed) this.configService.Save();
    }
}