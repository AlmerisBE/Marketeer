using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.UI.CompetitionTracking.UI;

public class CompetitionConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Configuration");
    public string Name => this.localization.Translate("Config_Competition_TabName");
    public int Priority => 100;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionConfigMenu(IConfigurationService configService, ILocalizationService localization) {
        this.configService = configService;
        this.localization = localization;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool isChanged = false;

        if (ImGui.CollapsingHeader(this.localization.Translate("Config_Pricing_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawUndercutSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localization.Translate("Config_Whitelist_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawWhitelistSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localization.Translate("Config_LossPrevention_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawLossPreventionSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localization.Translate("Config_Fallback_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawFallbackSettings(config);
        }

        if (isChanged) this.configService.Save();
    }

    private bool DrawUndercutSettings(PluginConfiguration config) {
        bool changed = false;

        if (ImGui.BeginCombo(this.localization.Translate("Config_UndercutMode"), this.localization.Translate($"Config_UndercutMode_{config.UndercutMode}"))) {
            foreach (UndercutMode mode in Enum.GetValues(typeof(UndercutMode))) {
                if (ImGui.Selectable(this.localization.Translate($"Config_UndercutMode_{mode}"), config.UndercutMode == mode)) {
                    config.UndercutMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        if (config.UndercutMode == UndercutMode.Absolute) {
            int amount = (int)config.UndercutAmount;
            if (ImGui.InputInt(this.localization.Translate("Config_UndercutAmount"), ref amount)) {
                if (amount < 0) amount = 0;
                config.UndercutAmount = (uint)amount;
                changed = true;
            }
        }
        else {
            float percentage = (float)config.UndercutRelativePercentage;
            if (ImGui.SliderFloat(this.localization.Translate("Config_UndercutPercentage"), ref percentage, 0.1f, 100.0f, "%.1f%%")) {
                config.UndercutRelativePercentage = Math.Round(percentage, 1);
                changed = true;
            }
        }

        return changed;
    }

    private bool DrawWhitelistSettings(PluginConfiguration config) {
        bool changed = false;

        if (ImGui.BeginCombo(this.localization.Translate("Config_WhitelistBehavior"), this.localization.Translate($"Config_WhitelistBehavior_{config.CompetitorWhitelistBehavior}"))) {
            foreach (WhitelistBehavior behavior in Enum.GetValues(typeof(WhitelistBehavior))) {
                if (ImGui.Selectable(this.localization.Translate($"Config_WhitelistBehavior_{behavior}"), config.CompetitorWhitelistBehavior == behavior)) {
                    config.CompetitorWhitelistBehavior = behavior;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        return changed;
    }

    private bool DrawLossPreventionSettings(PluginConfiguration config) {
        bool changed = false;

        bool enforceVendor = config.EnforceVendorPriceMinimum;
        if (ImGui.Checkbox(this.localization.Translate("Config_EnforceVendorPriceMinimum"), ref enforceVendor)) {
            config.EnforceVendorPriceMinimum = enforceVendor;
            changed = true;
        }

        if (!config.EnforceVendorPriceMinimum) ImGui.BeginDisabled();

        if (ImGui.BeginCombo(this.localization.Translate("Config_LossBehavior"), this.localization.Translate($"Config_LossBehavior_{config.LossBehavior}"))) {
            foreach (MinimumPriceBehavior behavior in Enum.GetValues(typeof(MinimumPriceBehavior))) {
                if (ImGui.Selectable(this.localization.Translate($"Config_LossBehavior_{behavior}"), config.LossBehavior == behavior)) {
                    config.LossBehavior = behavior;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        // Return inventory target setting (shown when cancellation behavior is active or applicable)
        if (ImGui.BeginCombo(this.localization.Translate("Config_CancelPriority_Label"), this.localization.Translate($"Config_CancelPriority_{config.CancelInventoryPriority}"))) {
            foreach (InventoryPriority priority in Enum.GetValues(typeof(InventoryPriority))) {
                if (ImGui.Selectable(this.localization.Translate($"Config_CancelPriority_{priority}"), config.CancelInventoryPriority == priority)) {
                    config.CancelInventoryPriority = priority;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        if (!config.EnforceVendorPriceMinimum) ImGui.EndDisabled();

        return changed;
    }

    private bool DrawFallbackSettings(PluginConfiguration config) {
        bool changed = false;

        if (ImGui.BeginCombo(this.localization.Translate("Config_FallbackMode"), this.localization.Translate($"Config_FallbackMode_{config.EmptyMarketFallbackMode}"))) {
            foreach (FallbackPricingMode mode in Enum.GetValues(typeof(FallbackPricingMode))) {
                if (ImGui.Selectable(this.localization.Translate($"Config_FallbackMode_{mode}"), config.EmptyMarketFallbackMode == mode)) {
                    config.EmptyMarketFallbackMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        float multiplier = (float)config.EmptyMarketFallbackMultiplier;
        if (ImGui.InputFloat(this.localization.Translate("Config_FallbackMultiplier"), ref multiplier, 0.1f, 1.0f, "%.1f")) {
            if (multiplier < 1.0f) multiplier = 1.0f;
            config.EmptyMarketFallbackMultiplier = Math.Round(multiplier, 1);
            changed = true;
        }

        return changed;
    }
}