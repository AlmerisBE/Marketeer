using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Universalis.UI;

public class UniversalisConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Menu_UniversalisCache");
    public int Priority => 103;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public UniversalisConfigMenu(IConfigurationService configService, ILocalizationService localizationService) {
        this.configService = configService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_UniversalisCacheMinutes") ?? "Universalis Cache Duration (Minutes)");
        ImGui.SetNextItemWidth(250f);

        int cacheMinutes = config.UniversalisCacheMinutes;
        if (ImGui.SliderInt("##UniversalisCacheMinutes", ref cacheMinutes, 10, 60)) {
            config.UniversalisCacheMinutes = cacheMinutes;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UniversalisCacheMinutes_Tooltip"));

        if (changed) this.configService.Save();
    }
}