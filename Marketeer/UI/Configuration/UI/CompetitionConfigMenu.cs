using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Configuration.UI;

public class CompetitionConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;
    private string newWhitelistName = string.Empty;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Tab_Competition");
    public int Priority => 102;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        int undercut = (int)config.UndercutAmount;
        if (ImGui.InputInt(this.localizationService.Translate("Config_UndercutAmount"), ref undercut)) {
            if (undercut < 0) undercut = 0;
            config.UndercutAmount = (uint)undercut;
            isChanged = true;
        }

        bool enforceVendorPrice = config.EnforceVendorPriceMinimum;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnforceVendorPriceMinimum"), ref enforceVendorPrice)) {
            config.EnforceVendorPriceMinimum = enforceVendorPrice;
            isChanged = true;
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnforceVendorPriceMinimum_Tooltip"));

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

        if (isChanged) this.configurationService.Save();
    }
}