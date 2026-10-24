using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.UI.Shell.Components;

public class NotificationSettingsComponent {
    private ILocalizationService localizationService;

    public NotificationSettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool isChanged = false;

        bool enableChat = config.EnableChatNotifications;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableChatNotifications") ?? "Enable global chat notifications", ref enableChat)) {
            config.EnableChatNotifications = enableChat;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableChatNotifications_Tooltip"));

        ImGui.Spacing();

        bool enableDebug = config.EnableDebugMode;
        if (ImGui.Checkbox(this.localizationService.Translate("Config_EnableDebugMode") ?? "Enable Debug mode", ref enableDebug)) {
            config.EnableDebugMode = enableDebug;
            isChanged = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_EnableDebugMode_Tooltip"));

        return isChanged;
    }
}