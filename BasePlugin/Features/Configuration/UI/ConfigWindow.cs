using BasePlugin.Features.Configuration.Contracts;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace BasePlugin.Features.Configuration.UI;

public class ConfigWindow : Window {
    private IConfigurationService configurationService;

    public ConfigWindow(IConfigurationService configurationService)
        : base("BasePlugin Configuration", ImGuiWindowFlags.None) {
        this.configurationService = configurationService;

        // Basic window properties
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(300, 150),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw() {
        var config = this.configurationService.GetConfig();
        var exampleValue = config.ExampleCheckbox;

        if (ImGui.Checkbox("Exemple de case à cocher", ref exampleValue)) {
            config.ExampleCheckbox = exampleValue;
            this.configurationService.Save();
        }
    }
}