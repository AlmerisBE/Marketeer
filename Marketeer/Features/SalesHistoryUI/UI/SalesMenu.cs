using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.SalesHistoryUI.Contracts;
using Marketeer.Features.SalesHistoryUI.Models;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.Features.SalesHistoryUI.UI;

public class SalesMenu : INavigationNode {
    private ILocalizationService localization;
    private ITextureProvider textureProvider;
    private ISalesDataPresenter presenter;
    private ISalesDataProvider dataProvider;

    private string searchQuery = string.Empty;
    private SalesSortColumn currentSortColumn = SalesSortColumn.Quantity;
    private bool isSortAscending = false;

    public string Name => this.localization.Translate("SalesTab_Title");
    public int Priority => 20;

    // INavigationNode implementation for a leaf node
    public bool HasContent => true;
    public bool DefaultExpanded => false;
    public IEnumerable<INavigationNode> GetChildren() => [];

    public SalesMenu(
        ILocalizationService localization,
        ITextureProvider textureProvider,
        ISalesDataPresenter presenter,
        ISalesDataProvider dataProvider) {

        this.localization = localization;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
        this.dataProvider = dataProvider;
    }

    public void DrawContent() {
        var searchPlaceholder = this.localization.Translate("SalesTab_SearchPlaceholder");
        ImGui.InputText($"##salesSearch", ref this.searchQuery, 256);

        if (ImGui.IsItemHovered() && string.IsNullOrEmpty(this.searchQuery)) {
            ImGui.SetTooltip(searchPlaceholder);
        }

        ImGui.Separator();

        var rawData = this.dataProvider.GetSalesData();
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
        if (iconId == 0) {
            return;
        }

        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();

        if (iconWrap != null) {
            ImGui.Image(iconWrap.Handle, new Vector2(24, 24));
        }
    }
}