using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Configuration.UI;

public class ConfigMenu : INavigationNode {
    private readonly IConfigurationService configurationService;
    private readonly ILocalizationService localizationService;
    private readonly IKeyState keyState;

    private string newWhitelistName = string.Empty;
    private bool isCapturingHotkey = false;

    public string GroupName => this.localizationService.Translate("Group_General");
    public string Name => this.localizationService.Translate("Config_TabName");
    public int Priority => 100;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public ConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService, IKeyState keyState) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
        this.keyState = keyState;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        int cache = config.UniversalisCacheMinutes;
        if (ImGui.InputInt(this.localizationService.Translate("Config_CacheLabel"), ref cache)) {
            if (cache < 0) cache = 0;
            config.UniversalisCacheMinutes = cache;
            isChanged = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        bool enableDelay = config.EnableAutomationDelay;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_AutomationDelayToggle"), ref enableDelay)) {
            config.EnableAutomationDelay = enableDelay;
            isChanged = true;
        }

        if (enableDelay) {
            ImGui.Indent();
            int min = config.AutomationDelayMin;
            int max = config.AutomationDelayMax;

            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt(this.localizationService.Translate("Config_AutomationDelayMin"), ref min)) {
                if (min < 0) min = 0;
                config.AutomationDelayMin = min;
                if (min > config.AutomationDelayMax) config.AutomationDelayMax = min;
                isChanged = true;
            }

            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt(this.localizationService.Translate("Config_AutomationDelayMax"), ref max)) {
                if (max < config.AutomationDelayMin) max = config.AutomationDelayMin;
                config.AutomationDelayMax = max;
                isChanged = true;
            }
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_WhitelistLabel"));

        bool autoWhitelist = config.AutoWhitelistOwnRetainers;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_AutoWhitelistOwnRetainers"), ref autoWhitelist)) {
            config.AutoWhitelistOwnRetainers = autoWhitelist;
            isChanged = true;
        }

        ImGui.Spacing();

        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##newWhitelist", ref this.newWhitelistName, 64);
        ImGui.SameLine();

        if (ImGui.Button(this.localizationService.Translate("Config_WhitelistAdd"))) {
            var name = this.newWhitelistName.Trim();
            if (!string.IsNullOrWhiteSpace(name) && !config.CompetitorWhitelist.Contains(name)) {
                config.CompetitorWhitelist.Add(name);
                this.newWhitelistName = string.Empty;
                isChanged = true;
            }
        }

        if (ImGui.BeginListBox("##whitelistBox", new Vector2(300f, 150f))) {
            for (int i = 0; i < config.CompetitorWhitelist.Count; i++) {
                ImGui.Selectable(config.CompetitorWhitelist[i], false);
                if (ImGui.BeginPopupContextItem($"whitelist_ctx_{i}")) {
                    if (ImGui.Selectable(this.localizationService.Translate("Config_WhitelistRemove"))) {
                        config.CompetitorWhitelist.RemoveAt(i);
                        isChanged = true;
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.EndListBox();
        }

        ImGui.TextUnformatted(this.localizationService.Translate("Config_WhitelistBehaviorLabel"));

        int behavior = (int)config.CompetitorWhitelistBehavior;

        if (ImGui.RadioButton(this.localizationService.Translate("Config_WhitelistBehavior_Ignore"), ref behavior, 0)) {
            config.CompetitorWhitelistBehavior = WhitelistBehavior.Ignore;
            isChanged = true;
        }

        if (ImGui.RadioButton(this.localizationService.Translate("Config_WhitelistBehavior_Match"), ref behavior, 1)) {
            config.CompetitorWhitelistBehavior = WhitelistBehavior.MatchPrice;
            isChanged = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_AdvancedHeader"));

        bool enforceVendorPrice = config.EnforceVendorPriceMinimum;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnforceVendorPriceMinimum"), ref enforceVendorPrice)) {
            config.EnforceVendorPriceMinimum = enforceVendorPrice;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip(this.localizationService.Translate("Config_EnforceVendorPriceMinimum_Tooltip"));
        }

        bool enableChatNotifications = config.EnableChatNotifications;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableChatNotifications"), ref enableChatNotifications)) {
            config.EnableChatNotifications = enableChatNotifications;
            isChanged = true;
        }

        bool enableDebugMode = config.EnableDebugMode;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableDebugMode"), ref enableDebugMode)) {
            config.EnableDebugMode = enableDebugMode;
            isChanged = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_HotkeyHeader"));
        ImGui.TextUnformatted(this.localizationService.Translate("Config_HotkeyLabel"));
        ImGui.SameLine();

        string keyName = config.DashboardHotkey == VirtualKey.NO_KEY ? this.localizationService.Translate("Config_HotkeyNone") : config.DashboardHotkey.ToString();

        if (this.isCapturingHotkey) {
            ImGui.Button(this.localizationService.Translate("Config_HotkeyWaiting"), new Vector2(250f, 0));

            foreach (var key in Enum.GetValues<VirtualKey>()) {
                if (this.keyState[key]) {
                    if (key == VirtualKey.ESCAPE) {
                        config.DashboardHotkey = VirtualKey.NO_KEY;
                        config.DashboardHotkeyCtrl = false;
                        config.DashboardHotkeyAlt = false;
                        config.DashboardHotkeyShift = false;
                        this.isCapturingHotkey = false;
                        isChanged = true;
                        break;
                    }

                    if (key != VirtualKey.CONTROL && key != VirtualKey.MENU && key != VirtualKey.SHIFT) {
                        config.DashboardHotkey = key;
                        config.DashboardHotkeyCtrl = this.keyState[VirtualKey.CONTROL];
                        config.DashboardHotkeyAlt = this.keyState[VirtualKey.MENU];
                        config.DashboardHotkeyShift = this.keyState[VirtualKey.SHIFT];
                        this.isCapturingHotkey = false;
                        isChanged = true;
                        break;
                    }
                }
            }
        }
        else {
            string modifierStr = "";
            if (config.DashboardHotkeyCtrl) modifierStr += "Ctrl + ";
            if (config.DashboardHotkeyAlt) modifierStr += "Alt + ";
            if (config.DashboardHotkeyShift) modifierStr += "Shift + ";

            if (ImGui.Button($"{modifierStr}{keyName}##hotkeyBtn", new Vector2(250f, 0))) {
                this.isCapturingHotkey = true;
            }
        }

        if (isChanged) this.configurationService.Save();
    }
}