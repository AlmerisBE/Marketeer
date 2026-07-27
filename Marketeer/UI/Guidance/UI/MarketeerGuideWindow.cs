using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Marketeer.API.Guidance.Contracts;
using System.Numerics;

namespace Marketeer.UI.Guidance.UI;

public class MarketeerGuideWindow : Window {
    private IWindowGeometryProvider geometryProvider;

    public MarketeerGuideWindow(IWindowGeometryProvider geometryProvider)
        : base("Marketeer Guide", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoFocusOnAppearing) {

        this.geometryProvider = geometryProvider;
        this.IsOpen = true;
    }

    public override bool DrawConditions() {
        return this.geometryProvider.GetWindowGeometry("RetainerSellList", out _, out _, out _, out _, out _);
    }

    public override void PreDraw() {
        if (this.geometryProvider.GetWindowGeometry("RetainerSellList", out var x, out var y, out _, out _, out _)) {
            float margin = 10f;
            float targetX = x - margin;
            ImGui.SetNextWindowPos(new Vector2(targetX, y), ImGuiCond.Always, new Vector2(1.0f, 0.0f));
        }
    }

    public override void Draw() {
        ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), "Marketeer Guide");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Waiting for instructions...");
    }
}