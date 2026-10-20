using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.MarketStrategy.UI;

public class MarketStrategyConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;
    private IDataManager dataManager;

    private Dictionary<uint, string> categoryNames = [];
    private string newProfileName = string.Empty;
    private string selectedProfileId = string.Empty;
    private int selectedCategoryId = 0;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Menu_MarketStrategy") ?? "Market Strategy";
    public int Priority => 101;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public MarketStrategyConfigMenu(IConfigurationService configService, ILocalizationService localizationService, IDataManager dataManager) {
        this.configService = configService;
        this.localizationService = localizationService;
        this.dataManager = dataManager;

        this.LoadCategories();
    }

    private void LoadCategories() {
        var sheet = this.dataManager.GetExcelSheet<ItemSearchCategory>();
        if (sheet == null) return;

        foreach (var cat in sheet) {
            var name = cat.Name.ToString();
            if (cat.RowId > 0 && !string.IsNullOrWhiteSpace(name)) this.categoryNames[cat.RowId] = name;
        }
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        if (this.DrawAnomalySettings(config)) changed = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.DrawGlobalPricingSettings(config)) changed = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.DrawProfileManagement(config)) changed = true;

        if (changed) this.configService.Save();
    }

    private bool DrawAnomalySettings(PluginConfiguration config) {
        bool changed = false;

        bool isEnabled = config.EnableAnomalyProtection;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableAnomalyProtection") ?? "Enable Market Crash Protection", ref isEnabled)) {
            config.EnableAnomalyProtection = isEnabled;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableAnomalyProtection_Tooltip"));

        if (isEnabled) {
            ImGui.Spacing();
            ImGui.Indent();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AnomalyCrashThreshold") ?? "Crash Threshold (%)");
            ImGui.SetNextItemWidth(250f);

            int thresholdPct = (int)(config.AnomalyCrashThreshold * 100);
            if (ImGui.SliderInt("##CrashThreshold", ref thresholdPct, 1, 99)) {
                config.AnomalyCrashThreshold = thresholdPct / 100.0;
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AnomalyCrashThreshold_Tooltip"));

            ImGui.Spacing();

            ImGui.TextUnformatted(this.localizationService.Translate("Config_AnomalyStrategy") ?? "Defense Strategy");
            ImGui.SetNextItemWidth(250f);

            if (ImGui.BeginCombo("##AnomalyStrategy", this.localizationService.Translate($"Config_AnomalyStrategy_{config.AnomalyStrategy}"))) {
                foreach (AnomalyDefenseStrategy strategy in Enum.GetValues(typeof(AnomalyDefenseStrategy))) {
                    if (ImGui.Selectable(this.localizationService.Translate($"Config_AnomalyStrategy_{strategy}"), config.AnomalyStrategy == strategy)) {
                        config.AnomalyStrategy = strategy;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AnomalyStrategy_Tooltip"));

            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

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

        return changed;
    }

    private bool DrawGlobalPricingSettings(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Label_GlobalHqSettings") ?? "Global HQ Settings");

        bool ignoreHq = config.GlobalIgnoreHq;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_GlobalIgnoreHq") ?? "Ignore HQ Status Globally", ref ignoreHq)) {
            config.GlobalIgnoreHq = ignoreHq;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_GlobalIgnoreHq_Tooltip"));

        if (!ignoreHq) {
            float premium = (float)config.GlobalHqPremiumPercentage;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(this.localizationService.Translate("Config_GlobalHqPremium") ?? "HQ Premium (%)", ref premium, 0f, 100f, "%.1f%%")) {
                config.GlobalHqPremiumPercentage = Math.Round(premium, 1);
                changed = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_GlobalHqPremium_Tooltip"));
        }

        return changed;
    }

    private bool DrawProfileManagement(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Label_Profiles") ?? "Pricing Profiles");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Label_Profiles_Tooltip"));

        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##ProfileSelector", config.PricingProfiles.TryGetValue(this.selectedProfileId, out var p) ? p.Name : "...")) {
            foreach (var kvp in config.PricingProfiles) {
                bool isSelected = this.selectedProfileId == kvp.Key;
                if (ImGui.Selectable(kvp.Value.Name, isSelected)) this.selectedProfileId = kvp.Key;
                if (isSelected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        ImGui.InputText("##NewProfileName", ref this.newProfileName, 64);

        ImGui.SameLine();
        if (ImGui.Button(this.localizationService.Translate("Button_Add") ?? "Add") && !string.IsNullOrWhiteSpace(this.newProfileName)) {
            var newProfile = new PricingProfile { Name = this.newProfileName.Trim() };
            config.PricingProfiles[newProfile.Id] = newProfile;
            this.selectedProfileId = newProfile.Id;
            this.newProfileName = string.Empty;
            changed = true;
        }

        if (!string.IsNullOrEmpty(this.selectedProfileId) && config.PricingProfiles.TryGetValue(this.selectedProfileId, out var profile)) {
            ImGui.SameLine();
            if (ImGui.Button(this.localizationService.Translate("Button_Delete") ?? "Delete")) {
                config.PricingProfiles.Remove(this.selectedProfileId);

                var catsToRemove = config.CategoryProfileMappings.Where(kvp => kvp.Value == this.selectedProfileId).Select(kvp => kvp.Key).ToList();
                foreach (var cat in catsToRemove) config.CategoryProfileMappings.Remove(cat);

                var itemsToRemove = config.ItemProfileMappings.Where(kvp => kvp.Value == this.selectedProfileId).Select(kvp => kvp.Key).ToList();
                foreach (var itm in itemsToRemove) config.ItemProfileMappings.Remove(itm);

                this.selectedProfileId = string.Empty;
                changed = true;
            }

            ImGui.Spacing();
            if (this.DrawProfileEditor(profile)) changed = true;

            ImGui.Spacing();
            if (this.DrawCategoryMappings(config)) changed = true;
        }

        return changed;
    }

    private bool DrawProfileEditor(PricingProfile profile) {
        bool changed = false;

        string headerText = string.Format(this.localizationService.Translate("Label_ProfileOverrides") ?? "Overrides for '{0}'", profile.Name);
        ImGui.TextUnformatted(headerText);

        bool hasIgnoreHqOverride = profile.Rule.IgnoreHq.HasValue;
        if (ImGui.Checkbox($"{this.localizationService.Translate("Override_IgnoreHq") ?? "Override Ignore HQ"}##cbIgnHq", ref hasIgnoreHqOverride)) {
            profile.Rule.IgnoreHq = hasIgnoreHqOverride ? false : null;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Override_IgnoreHq_Tooltip"));

        if (hasIgnoreHqOverride) {
            ImGui.SameLine();
            bool ignoreHqVal = profile.Rule.IgnoreHq!.Value;
            string labelIgnoreVal = this.localizationService.Translate("OverrideValue_IgnoreHq") ?? "Ignore HQ";
            if (ImGui.Checkbox($"{labelIgnoreVal}##valIgnHq", ref ignoreHqVal)) {
                profile.Rule.IgnoreHq = ignoreHqVal;
                changed = true;
            }
        }

        bool hasPremiumOverride = profile.Rule.HqPremiumPercentage.HasValue;
        if (ImGui.Checkbox($"{this.localizationService.Translate("Override_HqPremium") ?? "Override HQ Premium"}##cbPrem", ref hasPremiumOverride)) {
            profile.Rule.HqPremiumPercentage = hasPremiumOverride ? 0.0 : null;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Override_HqPremium_Tooltip"));

        if (hasPremiumOverride) {
            ImGui.SameLine();
            float premVal = (float)profile.Rule.HqPremiumPercentage!.Value;
            ImGui.SetNextItemWidth(150f);
            string labelPremVal = this.localizationService.Translate("OverrideValue_HqPremium") ?? "Premium (%)";
            if (ImGui.SliderFloat($"{labelPremVal}##valPrem", ref premVal, 0f, 100f, "%.1f%%")) {
                profile.Rule.HqPremiumPercentage = Math.Round(premVal, 1);
                changed = true;
            }
        }

        bool hasUndercutOverride = profile.Rule.UndercutAmount.HasValue;
        if (ImGui.Checkbox($"{this.localizationService.Translate("Override_UndercutAmount") ?? "Override Undercut Amount"}##cbUnd", ref hasUndercutOverride)) {
            profile.Rule.UndercutAmount = hasUndercutOverride ? 1u : null;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Override_UndercutAmount_Tooltip"));

        if (hasUndercutOverride) {
            ImGui.SameLine();
            int underVal = (int)profile.Rule.UndercutAmount!.Value;
            ImGui.SetNextItemWidth(150f);
            string labelUndercutVal = this.localizationService.Translate("OverrideValue_UndercutAmount") ?? "Amount";
            if (ImGui.InputInt($"{labelUndercutVal}##valUnd", ref underVal)) {
                if (underVal < 0) underVal = 0;
                profile.Rule.UndercutAmount = (uint)underVal;
                changed = true;
            }
        }

        return changed;
    }

    private bool DrawCategoryMappings(PluginConfiguration config) {
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Label_CategoryMappings") ?? "Assigned Categories");

        var assignedCategories = config.CategoryProfileMappings.Where(kvp => kvp.Value == this.selectedProfileId).ToList();

        if (assignedCategories.Count > 0) {
            foreach (var kvp in assignedCategories) {
                var catName = this.categoryNames.TryGetValue(kvp.Key, out var name) ? name : $"Unknown ({kvp.Key})";
                ImGui.BulletText(catName);
                ImGui.SameLine();
                if (ImGui.SmallButton($"X##remCat_{kvp.Key}")) {
                    config.CategoryProfileMappings.Remove(kvp.Key);
                    changed = true;
                }
            }
        }

        ImGui.Spacing();

        var unassignedCategories = this.categoryNames.Where(kvp => !config.CategoryProfileMappings.ContainsKey(kvp.Key)).OrderBy(kvp => kvp.Value).ToList();

        ImGui.SetNextItemWidth(250f);
        string comboPreview = this.selectedCategoryId != 0 && this.categoryNames.TryGetValue((uint)this.selectedCategoryId, out var previewName) ? previewName : "...";

        if (ImGui.BeginCombo("##AssignCatCombo", comboPreview)) {
            foreach (var cat in unassignedCategories) {
                if (ImGui.Selectable(cat.Value, this.selectedCategoryId == cat.Key)) this.selectedCategoryId = (int)cat.Key;
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button(this.localizationService.Translate("Button_Assign") ?? "Assign") && this.selectedCategoryId != 0) {
            config.CategoryProfileMappings[(uint)this.selectedCategoryId] = this.selectedProfileId;
            this.selectedCategoryId = 0;
            changed = true;
        }

        return changed;
    }
}