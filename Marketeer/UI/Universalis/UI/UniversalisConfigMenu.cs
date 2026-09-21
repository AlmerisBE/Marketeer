using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.Universalis.UI;

public class UniversalisConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Tab_Universalis");
    public int Priority => 101;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public UniversalisConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        int cache = config.UniversalisCacheMinutes;
        if (ImGui.InputInt(this.localizationService.Translate("Config_CacheLabel"), ref cache)) {
            if (cache < 0) cache = 0;
            config.UniversalisCacheMinutes = cache;
            isChanged = true;
        }

        if (isChanged) this.configurationService.Save();
    }
}