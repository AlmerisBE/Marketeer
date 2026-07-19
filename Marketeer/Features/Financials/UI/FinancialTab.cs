using Dalamud.Bindings.ImGui;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Localization.Contracts;
using System.Numerics;

namespace Marketeer.Features.Financials.UI;

public class FinancialsTab : IDashboardTab {
    private IFinancialService financialService;
    private ILocalizationService localizationService;

    public string Name => this.localizationService.Translate("Financials_TabName") ?? "Financials";

    public FinancialsTab(IFinancialService financialService, ILocalizationService localizationService) {
        this.financialService = financialService;
        this.localizationService = localizationService;
    }

    public void Draw() {
        var summary = this.financialService.GetFinancialSummary();

        ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), this.localizationService.Translate("Financials_LiquidGil"));
        ImGui.SameLine();
        ImGui.Text($"{summary.GrandTotalGil:N0} Gil");

        ImGui.TextColored(new Vector4(0.8f, 0.6f, 0.2f, 1.0f), this.localizationService.Translate("Financials_MarketValue"));
        ImGui.SameLine();
        ImGui.Text($"{summary.GrandTotalMarketValue:N0} Gil");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTable("FinancialTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_CharacterRetainer"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_LiquidGil"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_MarketValue"));
            ImGui.TableSetupColumn(this.localizationService.Translate("Financials_ColHeader_CombinedTotal"));
            ImGui.TableHeadersRow();

            foreach (var character in summary.Characters) {
                ulong charTotalGil = 0;
                ulong charTotalMarket = 0;

                foreach (var ret in character.Retainers.Values) {
                    charTotalGil += ret.GilHeld;
                    foreach (var listing in ret.MarketListings.Values) {
                        charTotalMarket += (ulong)listing.PricePerUnit * listing.Quantity;
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"[+] {character.CharacterName}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{charTotalGil:N0}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{charTotalMarket:N0}");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{(charTotalGil + charTotalMarket):N0}");

                foreach (var retainer in character.Retainers.Values) {
                    ulong retainerMarketValue = 0;
                    foreach (var listing in retainer.MarketListings.Values) {
                        retainerMarketValue += (ulong)listing.PricePerUnit * listing.Quantity;
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"      {retainer.Name}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{retainer.GilHeld:N0}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{retainerMarketValue:N0}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(retainer.GilHeld + retainerMarketValue):N0}");
                }
            }
            ImGui.EndTable();
        }
    }
}