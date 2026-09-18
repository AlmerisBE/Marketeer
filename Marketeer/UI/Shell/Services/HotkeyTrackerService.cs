using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
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

    private unsafe bool IsInputFocused() {
        try {
            if (ImGui.GetIO().WantCaptureKeyboard) return true;

            var uiModule = UIModule.Instance();
            if (uiModule != null) {
                var raptureAtkModule = uiModule->GetRaptureAtkModule();
                if (raptureAtkModule != null && raptureAtkModule->AtkModule.IsTextInputActive()) return true;
            }
        }
        catch {
            // Silently swallow exceptions to prevent framework crashes
        }

        return false;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        var config = this.configService.GetConfig();
        var hotkey = config.DashboardHotkey;

        if (hotkey == VirtualKey.NO_KEY) return;

        bool isPressed = this.keyState[hotkey];

        if (isPressed && !this.wasPressed && !this.IsInputFocused()) {
            bool ctrlPressed = this.keyState[VirtualKey.CONTROL];
            bool shiftPressed = this.keyState[VirtualKey.SHIFT];
            bool altPressed = this.keyState[VirtualKey.MENU];

            if (ctrlPressed == config.DashboardHotkeyCtrl &&
                shiftPressed == config.DashboardHotkeyShift &&
                altPressed == config.DashboardHotkeyAlt) {
                this.mainWindow.Toggle();
            }
        }

        this.wasPressed = isPressed;
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
}