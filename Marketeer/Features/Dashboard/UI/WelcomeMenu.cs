using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Marketeer.Features.Dashboard.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.Dashboard.UI;

public class WelcomeMenu : INavigationNode {
    private IDalamudPluginInterface pluginInterface;

    public string Name => "Accueil";
    public int Priority => 0; // Ensures it's at the top
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public WelcomeMenu(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        var version = this.pluginInterface.Manifest.AssemblyVersion.ToString();

        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"Marketeer v{version}");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("Bienvenue sur Marketeer ! Ce plugin vous permet de gérer et de suivre vos ventes au marché pour tous vos personnages et servants.");
        ImGui.Spacing();
        ImGui.TextWrapped("Utilisez le menu latéral pour naviguer entre vos personnages, surveiller vos statistiques financières, analyser l'historique de vos ventes, et suivre la concurrence en temps réel.");
    }
}