using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.SalesHistory.UI;

public class SalesStatisticsMenu : INavigationNode {
    private ISalesStatisticsService statisticsService;
    private IItemResolverService itemResolver;
    private ILocalizationService localization;

    public string GroupName => this.localization.Translate("Group_Statistics");
    public string Name => this.localization.Translate("SalesStatistics_TabName");
    public int Priority => 25;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public SalesStatisticsMenu(ISalesStatisticsService statisticsService, IItemResolverService itemResolver, ILocalizationService localization) {
        this.statisticsService = statisticsService;
        this.itemResolver = itemResolver;
        this.localization = localization;
    }

    public void DrawContent() {
        if (ImGui.BeginTabBar("SalesStatsTabs")) {
            if (ImGui.BeginTabItem(this.localization.Translate("SalesStatistics_TabSummary"))) {
                this.DrawSummary();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.localization.Translate("SalesStatistics_TabCharts"))) {
                this.DrawCharts();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSummary() {
        var summary = this.statisticsService.GetGlobalSummary();

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("SalesStatistics_GlobalOverview"));
        ImGui.Separator();
        ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_TotalSales", summary.TotalSalesCount.ToString("N0")));
        ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_ItemsSold", summary.TotalItemsSold.ToString("N0")));
        ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_AvgItemsPerSale", summary.AverageItemsPerSale.ToString("N2")));
        ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_TotalRevenue", summary.TotalRevenue.ToString("N0")));
        ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_AvgRevenuePerSale", summary.AverageRevenuePerSale.ToString("N0")));

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("SalesStatistics_TopSellers"));
        ImGui.Separator();

        var topSellers = this.statisticsService.GetTopBestSellers(10);
        if (ImGui.BeginTable("TopSellersTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColItemName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColRevenue"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            foreach (var item in topSellers) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(this.itemResolver.ResolveItemName(item.ItemId));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.TotalQuantitySold.ToString("N0"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{item.TotalRevenue:N0}");
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("SalesStatistics_FastestSellers"));
        ImGui.Separator();

        var fastest = this.statisticsService.GetFastestSellingItems(10);
        if (ImGui.BeginTable("FastestSellersTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColItemName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColSalesTracked"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("SalesStatistics_ColAvgTime"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            foreach (var item in fastest) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.ItemName);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.SalesCount.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(this.localization.Translate("SalesStatistics_Hours", item.AverageTimeToSell.TotalHours.ToString("N1")));
            }
            ImGui.EndTable();
        }
    }

    private unsafe void DrawCharts() {
        var chartData = this.statisticsService.GetDailySalesChartData(14).ToList();
        if (chartData.Count == 0) {
            return;
        }

        float[] revenueData = chartData.Select(d => (float)d.Revenue).ToArray();
        float[] salesData = chartData.Select(d => (float)d.SalesCount).ToArray();

        ImGui.Spacing();
        if (ImPlot.BeginPlot(this.localization.Translate("SalesStatistics_ChartRevenueTitle"), new Vector2(-1, 250))) {
            // Explicit cast to resolve CS0121 ambiguity
            ImPlot.SetupAxes((byte*)null, (byte*)null);

            fixed (float* pRev = revenueData) {
                ImPlot.PlotBars(this.localization.Translate("SalesStatistics_ColRevenue"), pRev, chartData.Count);
            }
            ImPlot.EndPlot();
        }

        ImGui.Spacing();
        if (ImPlot.BeginPlot(this.localization.Translate("SalesStatistics_ChartSalesCountTitle"), new Vector2(-1, 250))) {
            // Explicit cast to resolve CS0121 ambiguity
            ImPlot.SetupAxes((byte*)null, (byte*)null);

            fixed (float* pSales = salesData) {
                ImPlot.PlotBars(this.localization.Translate("SalesStatistics_ChartSalesCountLegend"), pSales, chartData.Count);
            }
            ImPlot.EndPlot();
        }
    }
}