using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Configuration.UI;

public class CompetitionConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Configuration");
    public string Name => this.localization.Translate("Config_Tab_Competition");
    public int Priority => 20;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public CompetitionConfigMenu(IConfigurationService configService, ILocalizationService localization) {
        this.configService = configService;
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool isModified = false;

        ImGui.TextUnformatted(this.localization.Translate("Config_Header_Competition"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localization.Translate("Config_AutoSellModifierKeyLabel"));

        this.DrawModifierCheckbox(config, ModifierKey.Ctrl, this.localization.Translate("ModifierKey_Ctrl"), ref isModified);
        ImGui.SameLine();
        this.DrawModifierCheckbox(config, ModifierKey.Shift, this.localization.Translate("ModifierKey_Shift"), ref isModified);
        ImGui.SameLine();
        this.DrawModifierCheckbox(config, ModifierKey.Alt, this.localization.Translate("ModifierKey_Alt"), ref isModified);

        if (isModified) this.configService.Save();
    }

    private void DrawModifierCheckbox(PluginConfiguration config, ModifierKey flag, string label, ref bool isModified) {
        bool isActive = config.AutoSellModifierKey.HasFlag(flag);

        if (ImGui.Checkbox(label, ref isActive)) {
            if (isActive) config.AutoSellModifierKey |= flag;
            else config.AutoSellModifierKey &= ~flag;

            isModified = true;
        }
    }
}