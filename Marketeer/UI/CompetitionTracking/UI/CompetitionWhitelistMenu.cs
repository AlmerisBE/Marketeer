using Dalamud.Bindings.ImGui;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.CompetitionTracking.UI;

public class CompetitionWhitelistMenu : INavigationNode {
    private IWhitelistManagerService whitelistManager;
    private ILocalizationService localization;
    private string newRetainerName = string.Empty;

    public string GroupName => this.localization.Translate("Group_Configuration") ?? "Configuration";
    public string Name => this.localization.Translate("Menu_CompetitionWhitelist") ?? "Whitelist";
    public int Priority => 110; // Placed right after the main configuration menu

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public CompetitionWhitelistMenu(IWhitelistManagerService whitelistManager, ILocalizationService localization) {
        this.whitelistManager = whitelistManager;
        this.localization = localization;
    }

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("Config_WhitelistedRetainers") ?? "Whitelisted Retainers:");

        if (ImGui.Button(this.localization.Translate("Config_Whitelist_AutoAdd") ?? "Auto-detect Own Retainers")) {
            this.whitelistManager.AutoAddKnownRetainers();
        }

        ImGui.Spacing();

        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##NewRetainerName", ref this.newRetainerName, 64);
        ImGui.SameLine();

        if (ImGui.Button(this.localization.Translate("Button_Add") ?? "Add") && !string.IsNullOrWhiteSpace(this.newRetainerName)) {
            this.whitelistManager.AddToWhitelist(this.newRetainerName);
            this.newRetainerName = string.Empty;
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("WhitelistTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit)) {
            ImGui.TableSetupColumn(this.localization.Translate("Config_Whitelist_ColRetainerName") ?? "Retainer Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("Config_Whitelist_ColActions") ?? "Actions", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            string? toRemove = null;

            foreach (var retainer in this.whitelistManager.GetWhitelistedRetainers()) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(retainer);

                ImGui.TableNextColumn();
                if (ImGui.Button($"{this.localization.Translate("Button_Remove") ?? "Remove"}##{retainer}")) {
                    toRemove = retainer;
                }
            }

            ImGui.EndTable();

            if (toRemove != null) this.whitelistManager.RemoveFromWhitelist(toRemove);
        }
    }
}