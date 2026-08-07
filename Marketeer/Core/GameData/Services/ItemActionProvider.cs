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

    // We now cache the raw internal X and Y coordinates along with the map IDs
    private ConcurrentDictionary<uint, (uint TerritoryId, uint MapId, int RawX, int RawY)?> gatheringLocationCache = new();

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
            // Using the raw coordinates extracted from the MapMarker sheet to pinpoint the node with a flag
            var payload = new MapLinkPayload(loc.Value.TerritoryId, loc.Value.MapId, loc.Value.RawX, loc.Value.RawY);
            this.gameGui.OpenMapWithMapLink(payload);
        }
    }

    private (uint TerritoryId, uint MapId, int RawX, int RawY)? GetGatheringLocation(uint itemId) {
        if (this.gatheringLocationCache.TryGetValue(itemId, out var cached)) {
            return cached;
        }

        var loc = this.ResolveGatheringLocation(itemId);
        this.gatheringLocationCache[itemId] = loc;

        return loc;
    }

    private (uint TerritoryId, uint MapId, int RawX, int RawY)? ResolveGatheringLocation(uint itemId) {
        var gatheringItemSheet = this.dataManager.GetExcelSheet<GatheringItem>();
        var gatheringPointBaseSheet = this.dataManager.GetExcelSheet<GatheringPointBase>();
        var gatheringPointSheet = this.dataManager.GetExcelSheet<GatheringPoint>();
        var territorySheet = this.dataManager.GetExcelSheet<TerritoryType>();
        var mapMarkerSheet = this.dataManager.GetSubrowExcelSheet<MapMarker>();

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
        uint? gatheringPointId = null;
        foreach (var p in gatheringPointSheet) {
            if (p.GatheringPointBase.RowId == pointBaseId.Value) {
                territoryId = p.TerritoryType.RowId;
                gatheringPointId = p.RowId;
                break;
            }
        }
        if (territoryId == null || territoryId.Value == 0 || gatheringPointId == null) {
            return null;
        }

        var terr = territorySheet.GetRowOrDefault(territoryId.Value);
        if (terr == null) {
            return null;
        }

        int rawX = 0;
        int rawY = 0;

        if (mapMarkerSheet != null) {
            // MapMarker is a subrow sheet, we must iterate through the row collections, then the subrows
            foreach (var markerCollection in mapMarkerSheet) {
                foreach (var marker in markerCollection) {
                    // DataType 3 and 4 are typical for fishing and standard gathering. We extract RowId from the DataKey RowRef.
                    if ((marker.DataType == 3 || marker.DataType == 4) && marker.DataKey.RowId == gatheringPointId.Value) {
                        rawX = marker.X;
                        rawY = marker.Y;
                        break;
                    }
                }
                if (rawX != 0 || rawY != 0) {
                    break;
                }
            }
        }

        return (territoryId.Value, terr.Value.Map.RowId, rawX, rawY);
    }
}