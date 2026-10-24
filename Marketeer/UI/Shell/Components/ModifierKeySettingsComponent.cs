using Dalamud.Bindings.ImGui;
using Marketeer.Core.Configuration.Models;
using Marketeer.UI.Localization.Contracts;

namespace Marketeer.UI.Shell.Components;

public class ModifierKeySettingsComponent {
    private ILocalizationService localizationService;

    public ModifierKeySettingsComponent(ILocalizationService localizationService) {
        this.localizationService = localizationService;
    }

    public bool Draw(PluginConfiguration config) {
        bool isChanged = false;

        ImGui.TextUnformatted(this.localizationService.Translate("Config_AutoSellModifierKeyLabel") ?? "Auto-adjust modifier key(s):");

        ImGui.BeginGroup();
        bool hasCtrl = config.AutoSellModifierKey.HasFlag(ModifierKey.Ctrl);
        bool hasAlt = config.AutoSellModifierKey.HasFlag(ModifierKey.Alt);
        bool hasShift = config.AutoSellModifierKey.HasFlag(ModifierKey.Shift);

        if (ImGui.Checkbox(this.localizationService.Translate("ModifierKey_Ctrl") ?? "Ctrl", ref hasCtrl)) {
            if (hasCtrl) config.AutoSellModifierKey |= ModifierKey.Ctrl;
            else config.AutoSellModifierKey &= ~ModifierKey.Ctrl;
            isChanged = true;
        }

        ImGui.SameLine();
        if (ImGui.Checkbox(this.localizationService.Translate("ModifierKey_Alt") ?? "Alt", ref hasAlt)) {
            if (hasAlt) config.AutoSellModifierKey |= ModifierKey.Alt;
            else config.AutoSellModifierKey &= ~ModifierKey.Alt;
            isChanged = true;
        }

        ImGui.SameLine();
        if (ImGui.Checkbox(this.localizationService.Translate("ModifierKey_Shift") ?? "Shift", ref hasShift)) {
            if (hasShift) config.AutoSellModifierKey |= ModifierKey.Shift;
            else config.AutoSellModifierKey &= ~ModifierKey.Shift;
            isChanged = true;
        }
        ImGui.EndGroup();

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(this.localizationService.Translate("Config_AutoSellModifierKey_Tooltip"));

        return isChanged;
    }
}