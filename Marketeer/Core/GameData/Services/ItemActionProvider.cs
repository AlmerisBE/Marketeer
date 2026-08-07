using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.GameData.Contracts;
using System.Linq;

namespace Marketeer.Core.GameData.Services;

public class ItemActionProvider : IItemActionProvider {
    private IDataManager dataManager;
    private IGameGui gameGui;
    private IRecipeDataService recipeDataService;

    public ItemActionProvider(IDataManager dataManager, IGameGui gameGui, IRecipeDataService recipeDataService) {
        this.dataManager = dataManager;
        this.gameGui = gameGui;
        this.recipeDataService = recipeDataService;
    }

    public bool CanOpenRecipe(uint itemId) {
        return this.recipeDataService.IsCraftable(itemId);
    }

    public unsafe void OpenRecipe(uint itemId) {
        var recipe = this.recipeDataService.GetPrimaryRecipe(itemId);
        if (recipe != null) {
            var agent = AgentRecipeNote.Instance();
            if (agent != null) {
                agent->OpenRecipeByRecipeId(recipe.RecipeId);
            }
        }
    }

    public bool CanOpenGatheringMap(uint itemId) {
        return this.GetGatheringLocation(itemId) != null;
    }

    public void OpenGatheringMap(uint itemId) {
        var loc = this.GetGatheringLocation(itemId);
        if (loc != null) {
            var payload = new MapLinkPayload(loc.Value.TerritoryId, loc.Value.MapId, 0f, 0f);
            this.gameGui.OpenMapWithMapLink(payload);
        }
    }

    private (uint TerritoryId, uint MapId)? GetGatheringLocation(uint itemId) {
        var gatheringItemSheet = this.dataManager.GetExcelSheet<GatheringItem>();
        var gatheringPointBaseSheet = this.dataManager.GetExcelSheet<GatheringPointBase>();
        var gatheringPointSheet = this.dataManager.GetExcelSheet<GatheringPoint>();
        var territorySheet = this.dataManager.GetExcelSheet<TerritoryType>();

        if (gatheringItemSheet == null || gatheringPointBaseSheet == null || gatheringPointSheet == null || territorySheet == null) {
            return null;
        }

        var gatheringItem = gatheringItemSheet.FirstOrDefault(g => g.Item.RowId == itemId);
        if (gatheringItem.RowId == 0 && gatheringItem.Item.RowId != itemId) {
            return null;
        }

        uint gatherItemId = gatheringItem.RowId;
        // Correction ici : utilisation de i.RowId au lieu de i pour extraire l'ID de la structure RowRef
        var pointBase = gatheringPointBaseSheet.FirstOrDefault(b => b.Item.Any(i => i.RowId == gatherItemId));
        if (pointBase.RowId == 0 && !pointBase.Item.Any(i => i.RowId == gatherItemId)) {
            return null;
        }

        var point = gatheringPointSheet.FirstOrDefault(p => p.GatheringPointBase.RowId == pointBase.RowId);
        if (point.RowId == 0 && point.GatheringPointBase.RowId != pointBase.RowId) {
            return null;
        }

        uint territoryId = point.TerritoryType.RowId;
        if (territoryId == 0) {
            return null;
        }

        var terr = territorySheet.GetRowOrDefault(territoryId);
        if (terr == null) {
            return null;
        }

        return (territoryId, terr.Value.Map.RowId);
    }
}