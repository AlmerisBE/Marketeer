using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.UI.Shell.UI;
using System;

namespace Marketeer.UI.Shell.Services;

public class HotkeyTrackerService : IDisposable {
    private IFramework framework;
    private IKeyState keyState;
    private IConfigurationService configService;
    private MainWindow mainWindow;

    private bool wasPressed;

    public HotkeyTrackerService(
        IFramework framework,
        IKeyState keyState,
        IConfigurationService configService,
        MainWindow mainWindow) {

        this.framework = framework;
        this.keyState = keyState;
        this.configService = configService;
        this.mainWindow = mainWindow;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        var config = this.configService.GetConfig();
        var hotkey = config.DashboardHotkey;

        if (hotkey == VirtualKey.NO_KEY) return;
        if (ImGui.GetIO().WantTextInput) return;

        bool isPressed = this.keyState[hotkey];
        bool ctrlPressed = this.keyState[VirtualKey.CONTROL];
        bool altPressed = this.keyState[VirtualKey.MENU];
        bool shiftPressed = this.keyState[VirtualKey.SHIFT];

        bool modifiersMatch = config.DashboardHotkeyCtrl == ctrlPressed &&
                              config.DashboardHotkeyAlt == altPressed &&
                              config.DashboardHotkeyShift == shiftPressed;

        if (isPressed && modifiersMatch) {
            if (!this.wasPressed) {
                this.wasPressed = true;
                this.mainWindow.Toggle();
            }
        }
        else if (!isPressed) {
            this.wasPressed = false;
        }
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}