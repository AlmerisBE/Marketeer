using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Dashboard.UI;

public class WelcomeMenu : INavigationNode {
    private IDalamudPluginInterface pluginInterface;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_General");
    public string Name => this.localizationService.Translate("Dashboard_WelcomeTab");
    public int Priority => 0;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public WelcomeMenu(IDalamudPluginInterface pluginInterface, ILocalizationService localizationService) {
        this.pluginInterface = pluginInterface;
        this.localizationService = localizationService;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        var version = this.pluginInterface.Manifest.AssemblyVersion.ToString();

        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"Marketeer v{version}");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(this.localizationService.Translate("Dashboard_WelcomeText1"));
        ImGui.Spacing();
        ImGui.TextWrapped(this.localizationService.Translate("Dashboard_WelcomeText2"));
    }
}