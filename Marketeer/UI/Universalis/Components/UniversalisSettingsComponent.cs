using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.UI.Universalis.Components;

public class UniversalisSettingsComponent {
    private ILocalizationService localizationService;

    public UniversalisSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool isChanged = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_UniversalisCacheMinutes") ?? "Cache Duration (Minutes)");
        ImGui.SetNextItemWidth(150f);

        int cacheMinutes = config.UniversalisCacheMinutes;
        if (ImGui.InputInt("##UniversalisCache", ref cacheMinutes)) {
            if (cacheMinutes < 1) cacheMinutes = 1;
            config.UniversalisCacheMinutes = cacheMinutes;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_UniversalisCacheMinutes_Tooltip"));

        return isChanged;
    }
}