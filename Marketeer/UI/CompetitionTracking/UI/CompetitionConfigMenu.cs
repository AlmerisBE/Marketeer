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
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Competition_TabName");
    public int Priority => 100;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionConfigMenu(IConfigurationService configService, ILocalizationService localizationService) {
        this.configService = configService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool isChanged = false;

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_Pricing_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawUndercutSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_Whitelist_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawWhitelistSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_LossPrevention_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawLossPreventionSettings(config);
        }

        if (ImGui.CollapsingHeader(this.localizationService.Translate("Config_Fallback_Header"), ImGuiTreeNodeFlags.DefaultOpen)) {
            isChanged |= this.DrawFallbackSettings(config);
        }

        if (isChanged) this.configService.Save();
    }

    private bool DrawUndercutSettings(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutMode"));
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##UndercutMode", this.localizationService.Translate($"Config_UndercutMode_{config.UndercutMode}"))) {
            foreach (UndercutMode mode in Enum.GetValues(typeof(UndercutMode))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_UndercutMode_{mode}"), config.UndercutMode == mode)) {
                    config.UndercutMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutMode_Tooltip"));

        ImGui.Spacing();

        if (config.UndercutMode == UndercutMode.Absolute) {
            ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutAmount"));
            ImGui.SetNextItemWidth(150f);
            int amount = (int)config.UndercutAmount;
            if (ImGui.InputInt("##UndercutAmount", ref amount)) {
                if (amount < 0) amount = 0;
                config.UndercutAmount = (uint)amount;
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutAmount_Tooltip"));
        }
        else {
            ImGui.TextUnformatted(this.localizationService.Translate("Config_UndercutPercentage"));
            ImGui.SetNextItemWidth(250f);
            float percentage = (float)config.UndercutRelativePercentage;
            if (ImGui.SliderFloat("##UndercutPercentage", ref percentage, 0.1f, 100.0f, "%.1f%%")) {
                config.UndercutRelativePercentage = Math.Round(percentage, 1);
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UndercutPercentage_Tooltip"));
        }

        ImGui.Spacing();
        return changed;
    }

    private bool DrawWhitelistSettings(PluginConfiguration config) {
        bool changed = false;

        bool autoWhitelist = config.AutoWhitelistOwnRetainers;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_AutoWhitelistOwnRetainers") ?? "Auto-whitelist own retainers", ref autoWhitelist)) {
            config.AutoWhitelistOwnRetainers = autoWhitelist;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AutoWhitelistOwnRetainers_Tooltip"));

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_WhitelistBehavior") ?? "Whitelist Behavior");
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##WhitelistBehavior", this.localizationService.Translate($"Config_WhitelistBehavior_{config.CompetitorWhitelistBehavior}"))) {
            foreach (WhitelistBehavior behavior in Enum.GetValues(typeof(WhitelistBehavior))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_WhitelistBehavior_{behavior}"), config.CompetitorWhitelistBehavior == behavior)) {
                    config.CompetitorWhitelistBehavior = behavior;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_WhitelistBehavior_Tooltip"));

        ImGui.Spacing();
        return changed;
    }

    private bool DrawLossPreventionSettings(PluginConfiguration config) {
        bool changed = false;

        bool enforceVendor = config.EnforceVendorPriceMinimum;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnforceVendorPriceMinimum"), ref enforceVendor)) {
            config.EnforceVendorPriceMinimum = enforceVendor;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnforceVendorPriceMinimum_Tooltip"));

        if (!config.EnforceVendorPriceMinimum) ImGui.BeginDisabled();

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_LossBehavior"));
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##LossBehavior", this.localizationService.Translate($"Config_LossBehavior_{config.LossBehavior}"))) {
            foreach (MinimumPriceBehavior behavior in Enum.GetValues(typeof(MinimumPriceBehavior))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_LossBehavior_{behavior}"), config.LossBehavior == behavior)) {
                    config.LossBehavior = behavior;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(this.localizationService.Translate("Config_LossBehavior_Tooltip"));

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_CancelPriority_Label"));
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##CancelPriority", this.localizationService.Translate($"Config_CancelPriority_{config.CancelInventoryPriority}"))) {
            foreach (InventoryPriority priority in Enum.GetValues(typeof(InventoryPriority))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_CancelPriority_{priority}"), config.CancelInventoryPriority == priority)) {
                    config.CancelInventoryPriority = priority;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(this.localizationService.Translate("Config_CancelPriority_Tooltip"));

        if (!config.EnforceVendorPriceMinimum) ImGui.EndDisabled();

        ImGui.Spacing();
        return changed;
    }

    private bool DrawFallbackSettings(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_FallbackMode"));
        ImGui.SetNextItemWidth(250f);
        if (ImGui.BeginCombo("##FallbackMode", this.localizationService.Translate($"Config_FallbackMode_{config.EmptyMarketFallbackMode}"))) {
            foreach (FallbackPricingMode mode in Enum.GetValues(typeof(FallbackPricingMode))) {
                if (ImGui.Selectable(this.localizationService.Translate($"Config_FallbackMode_{mode}"), config.EmptyMarketFallbackMode == mode)) {
                    config.EmptyMarketFallbackMode = mode;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_FallbackMode_Tooltip"));

        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_FallbackMultiplier"));
        ImGui.SetNextItemWidth(150f);
        float multiplier = (float)config.EmptyMarketFallbackMultiplier;
        if (ImGui.InputFloat("##FallbackMultiplier", ref multiplier, 0.1f, 1.0f, "%.1f")) {
            if (multiplier < 1.0f) multiplier = 1.0f;
            config.EmptyMarketFallbackMultiplier = Math.Round(multiplier, 1);
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_FallbackMultiplier_Tooltip"));

        ImGui.Spacing();
        return changed;
    }
}