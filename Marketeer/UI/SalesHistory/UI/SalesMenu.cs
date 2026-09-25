using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.Core.SalesHistory.Models;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.SalesHistory.UI;

public class SalesMenu : INavigationNode {
    private ILocalizationService localization;
    private ITextureProvider textureProvider;
    private ISalesDataPresenter presenter;
    private ISalesDataProvider dataProvider;
    private ISalesStatisticsService statisticsService;
    private IItemResolverService itemResolver;

    private string searchQuery = string.Empty;
    private SalesSortColumn currentSortColumn = SalesSortColumn.Quantity;
    private bool isSortAscending = false;
    private SalesPeriod currentPeriod = SalesPeriod.ThisWeek;

    public string GroupName => this.localization.Translate("Group_History") ?? "Analysis & History";
    public string Name => this.localization.Translate("Menu_SalesHistory") ?? "Sales History";
    public int Priority => 30;

    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public SalesMenu(
        ILocalizationService localization,
        ITextureProvider textureProvider,
        ISalesDataPresenter presenter,
        ISalesDataProvider dataProvider,
        ISalesStatisticsService statisticsService,
        IItemResolverService itemResolver) {

        this.localization = localization;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
        this.dataProvider = dataProvider;
        this.statisticsService = statisticsService;
        this.itemResolver = itemResolver;
    }

    public void DrawContent() {
        if (ImGui.BeginTabBar("SalesTabs")) {
            if (ImGui.BeginTabItem(this.localization.Translate("SalesTab_History") ?? "History")) {
                this.DrawHistory();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.localization.Translate("SalesStatistics_TabSummary") ?? "Summary")) {
                this.DrawSummary();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.localization.Translate("SalesStatistics_TabCharts") ?? "Charts")) {
                this.DrawCharts();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawHistory() {
        var searchPlaceholder = this.localization.Translate("SalesTab_SearchPlaceholder");

        ImGui.Spacing();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##periodCombo", this.currentPeriod.ToString())) {
            foreach (SalesPeriod period in Enum.GetValues(typeof(SalesPeriod))) {
                if (ImGui.Selectable(period.ToString(), this.currentPeriod == period)) {
                    this.currentPeriod = period;
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(256f);
        ImGui.InputText($"##salesSearch", ref this.searchQuery, 256);

        if (ImGui.IsItemHovered() && string.IsNullOrEmpty(this.searchQuery)) {
            ImGui.SetTooltip(searchPlaceholder);
        }

        ImGui.Separator();

        var rawData = this.dataProvider.GetSalesData(this.currentPeriod);
        var processedData = this.presenter.ProcessData(rawData, this.searchQuery, this.currentSortColumn, this.isSortAscending);

        this.DrawTable(processedData);
    }

    private void DrawTable(IReadOnlyList<ISalesViewRecord> data) {
        var tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInner;

        if (ImGui.BeginTable("SalesHistoryTable", 6, tableFlags)) {
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 24f);
            ImGui.TableSetupColumn(this.localization.Translate("SalesTab_ColName"), ImGuiTableColumnFlags.DefaultSort, 0f, (uint)SalesSortColumn.Name);
            ImGui.TableSetupColumn(this.localization.Translate("SalesTab_ColQuantity"), ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)SalesSortColumn.Quantity);
            ImGui.TableSetupColumn(this.localization.Translate("SalesTab_ColAvgPrice"), ImGuiTableColumnFlags.None, 0f, (uint)SalesSortColumn.AveragePrice);
            ImGui.TableSetupColumn(this.localization.Translate("SalesTab_ColTotal"), ImGuiTableColumnFlags.None, 0f, (uint)SalesSortColumn.TotalRevenue);
            ImGui.TableSetupColumn(this.localization.Translate("SalesTab_ColDate"), ImGuiTableColumnFlags.None, 0f, (uint)SalesSortColumn.LastSaleDate);
            ImGui.TableHeadersRow();

            this.HandleSorting();

            foreach (var item in data) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                this.DrawIcon(item.IconId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.TotalQuantitySold.ToString("N0"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.AverageUnitPrice.ToString("N0"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.TotalRevenue.ToString("N0"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.LastSaleDate.ToString("g"));
            }

            ImGui.EndTable();
        }
    }

    private void HandleSorting() {
        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty) {
            var specs = sortSpecs.Specs;
            this.currentSortColumn = (SalesSortColumn)specs.ColumnUserID;
            this.isSortAscending = specs.SortDirection == ImGuiSortDirection.Ascending;
            sortSpecs.SpecsDirty = false;
        }
    }

    private void DrawIcon(uint iconId) {
        if (iconId == 0) return;

        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();
        if (iconWrap != null) ImGui.Image(iconWrap.Handle, new Vector2(24, 24));
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
        if (chartData.Count == 0) return;

        double[] xPositions = Enumerable.Range(0, chartData.Count).Select(i => (double)i).ToArray();
        string[] xLabels = chartData.Select(d => d.Date.ToString("dd/MM")).ToArray();

        float[] revenueData = chartData.Select(d => (float)d.Revenue).ToArray();
        float[] salesData = chartData.Select(d => (float)d.SalesCount).ToArray();

        float maxRevenue = revenueData.Length > 0 ? revenueData.Max() : 0;
        float maxSales = salesData.Length > 0 ? salesData.Max() : 0;

        var plotFlags = ImPlotFlags.NoMouseText | ImPlotFlags.NoMenus | ImPlotFlags.NoBoxSelect;
        var axisFlags = ImPlotAxisFlags.Lock;

        ImGui.Spacing();
        if (ImPlot.BeginPlot(this.localization.Translate("SalesStatistics_ChartRevenueTitle"), new Vector2(-1, 250), plotFlags)) {
            ImPlot.SetupAxes((byte*)null, (byte*)null, axisFlags, axisFlags);
            ImPlot.SetupAxisLimits(ImAxis.X1, -0.6, chartData.Count - 0.4, ImPlotCond.Always);

            // Allow 20% margin at the top so the text labels on the highest bars aren't cut off
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, maxRevenue > 0 ? maxRevenue * 1.2 : 10, ImPlotCond.Always);

            ImPlot.SetupAxisTicks(ImAxis.X1, ref xPositions[0], chartData.Count, xLabels, false);

            fixed (float* pRev = revenueData) {
                ImPlot.PlotBars(this.localization.Translate("SalesStatistics_ColRevenue"), pRev, chartData.Count);
            }

            for (int i = 0; i < chartData.Count; i++) {
                if (revenueData[i] > 0) {
                    ImPlot.PlotText(revenueData[i].ToString("N0"), i, revenueData[i] + (maxRevenue * 0.05));
                }
            }
            ImPlot.EndPlot();
        }

        ImGui.Spacing();
        if (ImPlot.BeginPlot(this.localization.Translate("SalesStatistics_ChartSalesCountTitle"), new Vector2(-1, 250), plotFlags)) {
            ImPlot.SetupAxes((byte*)null, (byte*)null, axisFlags, axisFlags);
            ImPlot.SetupAxisLimits(ImAxis.X1, -0.6, chartData.Count - 0.4, ImPlotCond.Always);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, maxSales > 0 ? maxSales * 1.2 : 10, ImPlotCond.Always);

            ImPlot.SetupAxisTicks(ImAxis.X1, ref xPositions[0], chartData.Count, xLabels, false);

            fixed (float* pSales = salesData) {
                ImPlot.PlotBars(this.localization.Translate("SalesStatistics_ChartSalesCountLegend"), pSales, chartData.Count);
            }

            for (int i = 0; i < chartData.Count; i++) {
                if (salesData[i] > 0) {
                    ImPlot.PlotText(salesData[i].ToString("N0"), i, salesData[i] + (maxSales * 0.05));
                }
            }
            ImPlot.EndPlot();
        }
    }
}