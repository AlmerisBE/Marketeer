using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.GameData.Contracts;
using System.Collections.Concurrent;

namespace Marketeer.Core.GameData.Services;

public class ItemActionProvider : IItemActionProvider {
    private IDataManager dataManager;
    private IGameGui gameGui;
    private IRecipeDataService recipeDataService;
    private ConcurrentDictionary<uint, (uint TerritoryId, uint MapId)?> gatheringLocationCache = new();

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
        if (this.gatheringLocationCache.TryGetValue(itemId, out var cached)) {
            return cached;
        }

        var loc = this.ResolveGatheringLocation(itemId);
        this.gatheringLocationCache[itemId] = loc;

        return loc;
    }

    private (uint TerritoryId, uint MapId)? ResolveGatheringLocation(uint itemId) {
        var gatheringItemSheet = this.dataManager.GetExcelSheet<GatheringItem>();
        var gatheringPointBaseSheet = this.dataManager.GetExcelSheet<GatheringPointBase>();
        var gatheringPointSheet = this.dataManager.GetExcelSheet<GatheringPoint>();
        var territorySheet = this.dataManager.GetExcelSheet<TerritoryType>();

        if (gatheringItemSheet == null || gatheringPointBaseSheet == null || gatheringPointSheet == null || territorySheet == null) {
            return null;
        }

        uint? gatherItemId = null;
        foreach (var g in gatheringItemSheet) {
            if (g.Item.RowId == itemId) {
                gatherItemId = g.RowId;
                break;
            }
        }
        if (gatherItemId == null) {
            return null;
        }

        uint? pointBaseId = null;
        foreach (var b in gatheringPointBaseSheet) {
            foreach (var i in b.Item) {
                if (i.RowId == gatherItemId.Value) {
                    pointBaseId = b.RowId;
                    break;
                }
            }
            if (pointBaseId != null) {
                break;
            }
        }
        if (pointBaseId == null) {
            return null;
        }

        uint? territoryId = null;
        foreach (var p in gatheringPointSheet) {
            if (p.GatheringPointBase.RowId == pointBaseId.Value) {
                territoryId = p.TerritoryType.RowId;
                break;
            }
        }
        if (territoryId == null || territoryId.Value == 0) {
            return null;
        }

        var terr = territorySheet.GetRowOrDefault(territoryId.Value);
        if (terr == null) {
            return null;
        }

        return (territoryId.Value, terr.Value.Map.RowId);
    }
}