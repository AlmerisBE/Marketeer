using Dalamud.Bindings.ImGui;
using Marketeer.Core.Financials.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.Financials.UI;

public class FinancialsMenu : INavigationNode {
    private IFinancialService financialService;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Financials");
    public string Name => this.localizationService.Translate("Financials_TabName") ?? "Financials";
    public int Priority => 50;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public FinancialsMenu(IFinancialService financialService, ILocalizationService localizationService) {
        this.financialService = financialService;
        this.localizationService = localizationService;
    }

    public void DrawContent() {
        var summary = this.financialService.GetFinancialSummary();

        ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), this.localizationService.Translate("Financials_LiquidGil"));
        ImGui.SameLine();
        ImGui.TextUnformatted($"{summary.GrandTotalGil:N0} Gil");

        ImGui.TextColored(new Vector4(0.8f, 0.6f, 0.2f, 1.0f), this.localizationService.Translate("Financials_MarketValue"));
        ImGui.SameLine();
        ImGui.TextUnformatted($"{summary.GrandTotalMarketValue:N0} Gil");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTable("FinancialTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_CharacterRetainer"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_LiquidGil"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_MarketValue"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_CombinedTotal"));
            ImGui.TableHeadersRow();

            var inventoryLabel = this.localizationService.Translate("Financials_Inventory") ?? "(Inventory)";

            foreach (var character in summary.Characters) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"[+] {character.CharacterName}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{character.TotalGil:N0}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{character.TotalMarketValue:N0}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{(character.TotalGil + character.TotalMarketValue):N0}");

                // Display Character's native Gil
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"      {inventoryLabel}");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{character.CharacterGil:N0}");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("-");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{character.CharacterGil:N0}");

                foreach (var retainer in character.Retainers.Values) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"      {retainer.Name}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{retainer.GilHeld:N0}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{retainer.TotalMarketValue:N0}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{(retainer.GilHeld + retainer.TotalMarketValue):N0}");
                }
            }
            ImGui.EndTable();
        }
    }
}