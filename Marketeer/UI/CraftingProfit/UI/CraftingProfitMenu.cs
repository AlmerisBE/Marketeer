using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.CraftingProfit.Models;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.CraftingProfit.UI;

public class CraftingProfitMenu : INavigationNode {
    private ICraftingProfitRepository repository;
    private ICraftingProfitStateService stateService;
    private IItemResolverService itemResolver;
    private IMarketItemSearchProvider searchProvider;
    private IRecipeDataService recipeDataService;
    private ILocalizationService localization;

    private string searchInput = string.Empty;
    private ItemSearchResult? selectedItem;

    public string GroupName => this.localization.Translate("Group_Crafting");
    public string Name => this.localization.Translate("CraftingProfit_TabName");
    public int Priority => 60;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public CraftingProfitMenu(
        ICraftingProfitRepository repository,
        ICraftingProfitStateService stateService,
        IItemResolverService itemResolver,
        IMarketItemSearchProvider searchProvider,
        IRecipeDataService recipeDataService,
        ILocalizationService localization) {

        this.repository = repository;
        this.stateService = stateService;
        this.itemResolver = itemResolver;
        this.searchProvider = searchProvider;
        this.recipeDataService = recipeDataService;
        this.localization = localization;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    private void AddSelectedItem() {
        if (this.selectedItem != null) {
            if (this.repository.GetConfig(this.selectedItem.ItemId) == null) {
                var config = new CraftingItemConfig { ItemId = this.selectedItem.ItemId };
                this.repository.SaveConfig(config);
                _ = this.stateService.EvaluateItemAsync(config.ItemId);
            }
            this.selectedItem = null;
            this.searchInput = string.Empty;
        }
    }

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250f);
        string comboPreview = this.selectedItem != null ? this.selectedItem.Name : this.localization.Translate("CraftingProfit_SelectAnItem");

        if (ImGui.BeginCombo("##craftCombo", comboPreview)) {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##searchCraftInput", "...", ref this.searchInput, 100);

            var results = this.searchProvider.SearchMarketableItems(this.searchInput)
                .Where(r => this.recipeDataService.IsCraftable(r.ItemId));

            foreach (var res in results) {
                if (ImGui.Selectable(res.Name)) {
                    this.selectedItem = res;
                    this.searchInput = string.Empty;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button(this.localization.Translate("CraftingProfit_BtnAdd"))) {
            this.AddSelectedItem();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var configs = this.repository.GetAllConfigs().OrderBy(c => this.itemResolver.ResolveItemName(c.ItemId)).ToList();

        if (configs.Count == 0) {
            ImGui.TextDisabled(this.localization.Translate("CraftingProfit_NoItems"));
            return;
        }

        foreach (var config in configs) {
            this.DrawConfigCard(config);
            ImGui.Spacing();
        }
    }

    private void DrawConfigCard(CraftingItemConfig config) {
        var itemName = this.itemResolver.ResolveItemName(config.ItemId);
        this.stateService.Evaluations.TryGetValue(config.ItemId, out var evalResult);

        uint yieldQty = evalResult?.ResultQuantity ?? 1;
        string headerTitle = yieldQty > 1 ? $"{itemName} (x{yieldQty})" : itemName;

        int profit = evalResult?.Profit ?? 0;
        var headerColor = profit > 0 ? new Vector4(0.4f, 1.0f, 0.4f, 1.0f) : new Vector4(1.0f, 0.4f, 0.4f, 1.0f);

        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        bool isExpanded = ImGui.CollapsingHeader($"{headerTitle} - {this.localization.Translate("CraftingProfit_Profit", profit)}###craft_{config.ItemId}");
        ImGui.PopStyleColor();

        if (isExpanded) {
            ImGui.Indent();
            ImGui.Spacing();

            if (ImGuiComponents.IconButton((int)config.ItemId, FontAwesomeIcon.Trash)) {
                this.repository.RemoveConfig(config.ItemId);
                return;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_BtnRemove"));

            ImGui.Spacing();

            if (evalResult != null) {
                ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_MarketPrice", evalResult.CurrentMarketPrice));
                ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_CraftCost", evalResult.TotalCraftingCost));

                if (yieldQty > 1) {
                    ImGui.TextDisabled(this.localization.Translate("CraftingProfit_BatchCost", yieldQty, evalResult.BatchCraftingCost));
                }

                if (evalResult.TotalTargetCraftingCost > 0) {
                    ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_MaxCraftCost", evalResult.TotalTargetCraftingCost));
                }
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(this.localization.Translate("CraftingProfit_TargetSell"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            int targetSell = (int)config.TargetSellPrice;
            if (ImGui.InputInt($"##sell_{config.ItemId}", ref targetSell, 0, 0)) {
                config.TargetSellPrice = targetSell > 0 ? (uint)targetSell : 0;
                this.repository.SaveConfig(config);
                _ = this.stateService.EvaluateItemAsync(config.ItemId);
            }

            ImGui.Spacing();

            if (evalResult != null && evalResult.ComponentEvaluations != null && evalResult.ComponentEvaluations.Count > 0) {
                if (ImGui.BeginTable($"CraftingTable_{config.ItemId}", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable)) {
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColIngredient"), ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColQty"), ImGuiTableColumnFlags.WidthFixed, 40f);
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColUnitCost"), ImGuiTableColumnFlags.WidthFixed, 80f);
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColTotalCost"), ImGuiTableColumnFlags.WidthFixed, 80f);
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColMaxBuy"), ImGuiTableColumnFlags.WidthFixed, 100f);
                    ImGui.TableSetupColumn(this.localization.Translate("CraftingProfit_ColRecursion"), ImGuiTableColumnFlags.WidthFixed, 80f);
                    ImGui.TableHeadersRow();

                    foreach (var compEval in evalResult.ComponentEvaluations) {
                        this.DrawComponentRow(compEval, config);
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Unindent();
        }
    }

    private void DrawComponentRow(ComponentEvaluation eval, CraftingItemConfig rootConfig) {
        if (eval == null || rootConfig == null) {
            return;
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var compName = this.itemResolver.ResolveItemName(eval.ItemId);
        bool isNodeExpanded = false;

        if (eval.SubComponents != null && eval.SubComponents.Count > 0) {
            isNodeExpanded = ImGui.TreeNodeEx($"{compName}###compNode_{rootConfig.ItemId}_{eval.ItemId}", ImGuiTreeNodeFlags.DefaultOpen);
        }
        else {
            ImGui.TreeNodeEx($"{compName}###compNode_{rootConfig.ItemId}_{eval.ItemId}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
        }

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(eval.QuantityRequired.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(eval.UnitCost.ToString("N0"));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(eval.TotalCost.ToString("N0"));

        rootConfig.Components ??= new Dictionary<uint, ComponentConfig>();

        ComponentConfig safeCompConfig;
        if (!rootConfig.Components.TryGetValue(eval.ItemId, out var compConfig) || compConfig == null) {
            safeCompConfig = new ComponentConfig { ItemId = eval.ItemId };
            rootConfig.Components[eval.ItemId] = safeCompConfig;
        }
        else {
            safeCompConfig = compConfig;
        }

        ImGui.TableNextColumn();
        if (!safeCompConfig.CraftRecursively) {
            ImGui.SetNextItemWidth(-1);
            int maxBuy = (int)safeCompConfig.TargetBuyPrice;
            if (ImGui.InputInt($"##buy_{rootConfig.ItemId}_{eval.ItemId}", ref maxBuy, 0, 0)) {
                safeCompConfig.TargetBuyPrice = maxBuy > 0 ? (uint)maxBuy : 0;
                this.repository.SaveConfig(rootConfig);
                _ = this.stateService.EvaluateItemAsync(rootConfig.ItemId);
            }
        }
        else {
            ImGui.TextDisabled("-");
        }

        ImGui.TableNextColumn();
        if (this.recipeDataService.IsCraftable(eval.ItemId)) {
            bool craftRec = safeCompConfig.CraftRecursively;
            if (ImGui.Checkbox($"##rec_{rootConfig.ItemId}_{eval.ItemId}", ref craftRec)) {
                safeCompConfig.CraftRecursively = craftRec;
                if (craftRec) {
                    safeCompConfig.TargetBuyPrice = 0;
                }
                this.repository.SaveConfig(rootConfig);
                _ = this.stateService.EvaluateItemAsync(rootConfig.ItemId);
            }
        }
        else {
            ImGui.TextDisabled("-");
        }

        if (eval.SubComponents != null && eval.SubComponents.Count > 0 && isNodeExpanded) {
            foreach (var sub in eval.SubComponents) {
                this.DrawComponentRow(sub, rootConfig);
            }
            ImGui.TreePop();
        }
    }
}