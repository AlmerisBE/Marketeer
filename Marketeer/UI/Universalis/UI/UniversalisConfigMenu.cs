using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Universalis.UI;

public class UniversalisConfigMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Configuration");
    public string Name => this.localization.Translate("Menu_UniversalisCache");
    public int Priority => 101;
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public UniversalisConfigMenu(IConfigurationService configService, ILocalizationService localization) {
        this.configService = configService;
        this.localization = localization;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        bool changed = false;

        int cacheMinutes = config.UniversalisCacheMinutes;

        if (ImGui.SliderInt(this.localization.Translate("Config_UniversalisCacheMinutes") ?? "Universalis Cache Duration (Minutes)", ref cacheMinutes, 10, 60)) {
            config.UniversalisCacheMinutes = cacheMinutes;
            changed = true;
        }

        if (changed) this.configService.Save();
    }
}