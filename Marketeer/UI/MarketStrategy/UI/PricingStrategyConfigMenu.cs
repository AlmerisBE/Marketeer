using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.Components;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.MarketStrategy.UI;

public class PricingStrategyConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    private UndercutSettingsComponent undercutComponent;
    private MarketAnomalySettingsComponent anomalyComponent;
    private LossPreventionSettingsComponent lossPreventionComponent;
    private StorageAndOutlierSettingsComponent protectionComponent;
    private FallbackSettingsComponent fallbackComponent;
    private GlobalPricingSettingsComponent globalPricingComponent;
    private PricingProfileManagementComponent profileManagementComponent;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Config_Competition_TabName") ?? "Pricing Strategy";
    public int Priority => 100;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public PricingStrategyConfigMenu(IConfigurationService configService, ILocalizationService localizationService, IDataManager dataManager) {
        this.configService = configService;
        this.localizationService = localizationService;

        this.undercutComponent = new UndercutSettingsComponent(localizationService);
        this.anomalyComponent = new MarketAnomalySettingsComponent(localizationService);
        this.lossPreventionComponent = new LossPreventionSettingsComponent(localizationService);
        this.protectionComponent = new StorageAndOutlierSettingsComponent(localizationService);
        this.fallbackComponent = new FallbackSettingsComponent(localizationService);
        this.globalPricingComponent = new GlobalPricingSettingsComponent(localizationService);
        this.profileManagementComponent = new PricingProfileManagementComponent(localizationService, dataManager);
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_Pricing_Header") ?? "Undercut Settings", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.undercutComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_AnomalyProtection_Header") ?? "Market Crash Protection", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.anomalyComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_LossPrevention_Header") ?? "Loss Prevention", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.lossPreventionComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_StorageProtection") ?? "Storage & Outlier Protection", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.protectionComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_Fallback_Header") ?? "Empty Market Fallback", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.fallbackComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_GlobalHqSettings") ?? "Global HQ Settings", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.globalPricingComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Label_Profiles") ?? "Pricing Profiles", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (this.profileManagementComponent.Draw(config)) changed = true;
            ImGui.Spacing();
        }

        if (changed) this.configService.Save();
    }
}