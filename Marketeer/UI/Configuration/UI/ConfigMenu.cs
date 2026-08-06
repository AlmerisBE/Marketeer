using Dalamud.Bindings.ImGui;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Localization.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Configuration.UI;

public class ConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;
    private string newWhitelistName = string.Empty;

    public string GroupName => this.localizationService.Translate("Group_General");
    public string Name => this.localizationService.Translate("Config_TabName");
    public int Priority => 100;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public ConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        int cache = config.UniversalisCacheMinutes;
        if (ImGui.InputInt(this.localizationService.Translate("Config_CacheLabel"), ref cache)) {
            if (cache < 0) {
                cache = 0;
            }

            config.UniversalisCacheMinutes = cache;
            isChanged = true;
        }

        int watchInterval = config.MarketWatchPollingIntervalMinutes;
        if (ImGui.InputInt(this.localizationService.Translate("Config_MarketWatchIntervalLabel"), ref watchInterval)) {
            if (watchInterval < 0) {
                watchInterval = 0;
            }

            config.MarketWatchPollingIntervalMinutes = watchInterval;
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
                if (min < 0) {
                    min = 0;
                }

                config.AutomationDelayMin = min;
                if (min > config.AutomationDelayMax) {
                    config.AutomationDelayMax = min;
                }

                isChanged = true;
            }

            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt(this.localizationService.Translate("Config_AutomationDelayMax"), ref max)) {
                if (max < config.AutomationDelayMin) {
                    max = config.AutomationDelayMin;
                }

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

        if (isChanged) {
            this.configurationService.Save();
        }
    }
}