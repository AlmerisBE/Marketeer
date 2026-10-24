using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.UI.MarketStrategy.Components;

public class StorageAndOutlierSettingsComponent {
    private ILocalizationService localizationService;

    public StorageAndOutlierSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Label_StorageProtection") ?? "Storage & Outlier Protection");

        bool enableStorage = config.EnableStorageProtection;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableStorageProtection") ?? "Ignore Storage Listings", ref enableStorage)) {
            config.EnableStorageProtection = enableStorage;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableStorageProtection_Tooltip"));

        if (enableStorage) {
            ImGui.Indent();
            ImGui.Spacing();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_StorageProtectionThreshold") ?? "Threshold (Gil)");
            ImGui.SetNextItemWidth(150f);

            int threshold = (int)config.StorageProtectionThreshold;
            if (ImGui.InputInt("##StorageThreshold", ref threshold, 1000000, 5000000)) {
                if (threshold < 100000) threshold = 100000;
                config.StorageProtectionThreshold = (uint)threshold;
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_StorageProtectionThreshold_Tooltip"));

            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Indent();

        double outlierMultiplier = config.OutlierMultiplier;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.InputDouble("##OutlierMultiplier", ref outlierMultiplier, 1.0, 5.0, "%.1f")) {
            if (outlierMultiplier < 1.0) outlierMultiplier = 1.0;
            config.OutlierMultiplier = outlierMultiplier;
            changed = true;
        }
        ImGui.SameLine();
        ImGui.TextUnformatted(this.localizationService.Translate("Config_OutlierMultiplier") ?? "Outlier Multiplier (x Average)");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_OutlierMultiplier_Tooltip"));

        int outlierFloor = (int)config.OutlierMinimumFloor;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.InputInt("##OutlierFloor", ref outlierFloor, 1000, 5000)) {
            if (outlierFloor < 0) outlierFloor = 0;
            config.OutlierMinimumFloor = (uint)outlierFloor;
            changed = true;
        }
        ImGui.SameLine();
        ImGui.TextUnformatted(this.localizationService.Translate("Config_OutlierMinimumFloor") ?? "Minimum Floor (Gil)");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_OutlierMinimumFloor_Tooltip"));

        ImGui.Unindent();

        return changed;
    }
}