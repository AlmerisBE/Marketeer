using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Shell.UI;

public class HotkeyConfigMenu : INavigationNode {
    private IConfigurationService configurationService;
    private ILocalizationService localizationService;
    private IKeyState keyState;

    private bool isCapturingHotkey = false;

    public string GroupName => this.localizationService.Translate("Group_Configuration");
    public string Name => this.localizationService.Translate("Config_Tab_Hotkeys");
    public int Priority => 104;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public HotkeyConfigMenu(IConfigurationService configurationService, ILocalizationService localizationService, IKeyState keyState) {
        this.configurationService = configurationService;
        this.localizationService = localizationService;
        this.keyState = keyState;
    }

    public void DrawContent() {
        var config = this.configurationService.GetConfig();
        bool isChanged = false;

        ImGui.TextUnformatted(this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(this.localizationService.Translate("Config_HotkeyLabel"));
        ImGui.SameLine();

        string keyName = config.DashboardHotkey == VirtualKey.NO_KEY ? this.localizationService.Translate("Config_HotkeyNone") : config.DashboardHotkey.ToString();

        bool ctrlPressed = this.keyState[VirtualKey.CONTROL] || this.keyState[VirtualKey.LCONTROL] || this.keyState[VirtualKey.RCONTROL];
        bool altPressed = this.keyState[VirtualKey.MENU] || this.keyState[VirtualKey.LMENU] || this.keyState[VirtualKey.RMENU];
        bool shiftPressed = this.keyState[VirtualKey.SHIFT] || this.keyState[VirtualKey.LSHIFT] || this.keyState[VirtualKey.RSHIFT];

        if (this.isCapturingHotkey) {
            ImGui.Button(this.localizationService.Translate("Config_HotkeyWaiting"), new Vector2(250f, 0));

            foreach (var key in this.keyState.GetValidVirtualKeys()) {
                if (this.keyState[key]) {
                    if (key == VirtualKey.ESCAPE) {
                        config.DashboardHotkey = VirtualKey.NO_KEY;
                        config.DashboardHotkeyCtrl = false;
                        config.DashboardHotkeyAlt = false;
                        config.DashboardHotkeyShift = false;
                        this.isCapturingHotkey = false;
                        isChanged = true;
                        break;
                    }

                    if (key != VirtualKey.CONTROL && key != VirtualKey.LCONTROL && key != VirtualKey.RCONTROL &&
                        key != VirtualKey.MENU && key != VirtualKey.LMENU && key != VirtualKey.RMENU &&
                        key != VirtualKey.SHIFT && key != VirtualKey.LSHIFT && key != VirtualKey.RSHIFT) {

                        config.DashboardHotkey = key;
                        config.DashboardHotkeyCtrl = ctrlPressed;
                        config.DashboardHotkeyAlt = altPressed;
                        config.DashboardHotkeyShift = shiftPressed;
                        this.isCapturingHotkey = false;
                        isChanged = true;
                        break;
                    }
                }
            }
        }
        else {
            string modifierStr = "";
            if (config.DashboardHotkeyCtrl) modifierStr += "Ctrl + ";
            if (config.DashboardHotkeyAlt) modifierStr += "Alt + ";
            if (config.DashboardHotkeyShift) modifierStr += "Shift + ";

            if (ImGui.Button($"{modifierStr}{keyName}##hotkeyBtn", new Vector2(250f, 0))) {
                this.isCapturingHotkey = true;
            }
        }

        if (isChanged) this.configurationService.Save();
    }
}