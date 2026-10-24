using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.MarketStrategy.Components;

public class PricingProfileManagementComponent {
    private ILocalizationService localizationService;
    private IDataManager dataManager;

    private Dictionary<uint, string> categoryNames = [];
    private string newProfileName = string.Empty;
    private string selectedProfileId = string.Empty;
    private int selectedCategoryId = 0;

    public PricingProfileManagementComponent(ILocalizationService localizationService, IDataManager dataManager) {
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

    public bool Draw(PluginConfiguration config) {
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
            if (this.DrawProfileEditor(profile, config)) changed = true;

            ImGui.Spacing();
            if (this.DrawCategoryMappings(config)) changed = true;
        }

        return changed;
    }

    private bool DrawProfileEditor(PricingProfile profile, PluginConfiguration config) {
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

        bool hasOutlierMultOverride = profile.Rule.OutlierMultiplier.HasValue;
        if (ImGui.Checkbox($"{this.localizationService.Translate("Override_OutlierMultiplier") ?? "Override Outlier Multiplier"}##cbOutMult", ref hasOutlierMultOverride)) {
            profile.Rule.OutlierMultiplier = hasOutlierMultOverride ? config.OutlierMultiplier : null;
            changed = true;
        }

        if (hasOutlierMultOverride) {
            ImGui.SameLine();
            double multVal = profile.Rule.OutlierMultiplier!.Value;
            ImGui.SetNextItemWidth(150f);
            if (ImGui.InputDouble("##valOutMult", ref multVal, 1.0, 5.0, "%.1f")) {
                if (multVal < 1.0) multVal = 1.0;
                profile.Rule.OutlierMultiplier = multVal;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(this.localizationService.Translate("OverrideValue_OutlierMultiplier") ?? "Multiplier");
        }

        bool hasOutlierFloorOverride = profile.Rule.OutlierMinimumFloor.HasValue;
        if (ImGui.Checkbox($"{this.localizationService.Translate("Override_OutlierFloor") ?? "Override Minimum Floor"}##cbOutFloor", ref hasOutlierFloorOverride)) {
            profile.Rule.OutlierMinimumFloor = hasOutlierFloorOverride ? config.OutlierMinimumFloor : null;
            changed = true;
        }

        if (hasOutlierFloorOverride) {
            ImGui.SameLine();
            int floorVal = (int)profile.Rule.OutlierMinimumFloor!.Value;
            ImGui.SetNextItemWidth(150f);
            if (ImGui.InputInt("##valOutFloor", ref floorVal, 1000, 5000)) {
                if (floorVal < 0) floorVal = 0;
                profile.Rule.OutlierMinimumFloor = (uint)floorVal;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(this.localizationService.Translate("OverrideValue_OutlierFloor") ?? "Floor (Gil)");
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