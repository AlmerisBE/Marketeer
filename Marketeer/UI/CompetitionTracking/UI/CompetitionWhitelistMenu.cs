using Dalamud.Bindings.ImGui;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.MarketStrategy.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;

namespace Marketeer.UI.CompetitionTracking.UI;

public class CompetitionWhitelistMenu : INavigationNode {
    private IWhitelistManagerService whitelistManager;
    private IConfigurationService configService;
    private ILocalizationService localizationService;
    private string newRetainerName = string.Empty;

    public string GroupName => this.localizationService.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localizationService.Translate("Menu_CompetitionWhitelist") ?? "Whitelist";
    public int Priority => 102;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionWhitelistMenu(IWhitelistManagerService whitelistManager, IConfigurationService configService, ILocalizationService localizationService) {
        this.whitelistManager = whitelistManager;
        this.configService = configService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
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

        if (changed) this.configService.Save();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_WhitelistedRetainers") ?? "Whitelisted Retainers:");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##NewRetainerName", ref this.newRetainerName, 64);
        ImGui.SameLine();

        if (ImGui.Button(this.localizationService.Translate("Button_Add") ?? "Add") && !string.IsNullOrWhiteSpace(this.newRetainerName)) {
            this.whitelistManager.AddToWhitelist(this.newRetainerName.Trim());
            this.newRetainerName = string.Empty;
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("WhitelistTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("Config_Whitelist_ColRetainerName") ?? "Retainer Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localizationService.Translate("Config_Whitelist_ColActions") ?? "Actions", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            string? toRemove = null;

            foreach (var retainer in this.whitelistManager.GetWhitelistedRetainers()) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(retainer);

                ImGui.TableNextColumn();
                if (ImGui.Button($"{this.localizationService.Translate("Button_Remove") ?? "Remove"}##{retainer}")) {
                    toRemove = retainer;
                }
            }

            ImGui.EndTable();

            if (toRemove != null) this.whitelistManager.RemoveFromWhitelist(toRemove);
        }
    }
}