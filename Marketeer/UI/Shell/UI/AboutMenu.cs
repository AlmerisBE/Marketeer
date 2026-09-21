using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Shell.UI;

public class AboutMenu : INavigationNode {
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_General");
    public string Name => this.localization.Translate("About_TabName");
    public int Priority => 5;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public AboutMenu(ILocalizationService localization) {
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("About_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(this.localization.Translate("About_Description"));
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), this.localization.Translate("About_FeaturesHeader"));
        ImGui.BulletText(this.localization.Translate("About_Feature1"));
        ImGui.BulletText(this.localization.Translate("About_Feature2"));
        ImGui.BulletText(this.localization.Translate("About_Feature3"));
        ImGui.BulletText(this.localization.Translate("About_Feature4"));

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("About_LinksHeader"));
        ImGui.Spacing();

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.CodeBranch, this.localization.Translate("About_Github"))) {
            Util.OpenLink("https://github.com/AlmerisBE/Marketeer");
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comments, this.localization.Translate("About_Discord"))) {
            Util.OpenLink("https://discord.gg/2pgppfVmCu");
        }
    }
}