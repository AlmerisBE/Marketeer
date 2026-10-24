using Dalamud.Bindings.ImGui;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;

namespace Marketeer.UI.Shell.UI;

public class UniversalisStatusBarItem : IStatusBarProvider {
    private IUniversalisUpdateState state;
    private ILocalizationService localization;

    public int Priority => 1000;

    // Flag this component for specific alignment handling in the main window
    public bool IsRightAligned => true;

    public UniversalisStatusBarItem(IUniversalisUpdateState state, ILocalizationService localization) {
        this.state = state;
        this.localization = localization;
    }

    public void Draw() {
        string text;
        if (this.state.IsUpdating) {
            text = this.localization.Translate("StatusBar_Universalis_Updating") ?? "Universalis: Updating...";
        }
        else if (this.state.LastUpdateTime.HasValue) {
            var diff = DateTime.UtcNow - this.state.LastUpdateTime.Value;
            string timeStr = this.FormatTimeSpan(diff);
            text = string.Format(this.localization.Translate("StatusBar_Universalis_LastUpdate") ?? "Universalis: {0} ago", timeStr);
        }
        else text = this.localization.Translate("StatusBar_Universalis_Pending") ?? "Universalis: Waiting...";

        float textWidth = ImGui.CalcTextSize(text).X;
        float targetX = ImGui.GetWindowWidth() - textWidth - ImGui.GetStyle().WindowPadding.X;

        // Push cursor to the right
        if (targetX > ImGui.GetCursorPosX()) ImGui.SetCursorPosX(targetX);

        ImGui.TextDisabled(text);
    }

    private string FormatTimeSpan(TimeSpan span) {
        if (span.TotalMinutes < 1) return this.localization.Translate("Time_JustNow") ?? "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
        return $"{(int)span.TotalDays}d";
    }
}