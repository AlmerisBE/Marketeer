using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Shell.UI;

public class WelcomeMenu : INavigationNode {
    private IDalamudPluginInterface pluginInterface;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_General");
    public string Name => this.localization.Translate("Dashboard_WelcomeTab");
    public int Priority => 0;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public WelcomeMenu(IDalamudPluginInterface pluginInterface, ILocalizationService localization) {
        this.pluginInterface = pluginInterface;
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        var version = this.pluginInterface.Manifest.AssemblyVersion.ToString();

        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"Marketeer v{version}");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(this.localization.Translate("Dashboard_WelcomeText1"));
        ImGui.Spacing();
        ImGui.TextWrapped(this.localization.Translate("Dashboard_WelcomeText2"));

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        this.DrawSection("Welcome_Sec_Guidance_Title", "Welcome_Sec_Guidance_Desc");
        this.DrawSection("Welcome_Sec_Chars_Title", "Welcome_Sec_Chars_Desc");
        this.DrawSection("Welcome_Sec_Sales_Title", "Welcome_Sec_Sales_Desc");
        this.DrawSection("Welcome_Sec_History_Title", "Welcome_Sec_History_Desc");
        this.DrawSection("Welcome_Sec_Watch_Title", "Welcome_Sec_Watch_Desc");
        this.DrawSection("Welcome_Sec_Crafting_Title", "Welcome_Sec_Crafting_Desc");
        this.DrawSection("Welcome_Sec_Config_Title", "Welcome_Sec_Config_Desc");
    }

    private void DrawSection(string titleKey, string descKey) {
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.4f, 1.0f), this.localization.Translate(titleKey));
        ImGui.TextWrapped(this.localization.Translate(descKey));
        ImGui.Spacing();
    }
}