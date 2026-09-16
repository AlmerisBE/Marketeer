using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.API.InventoryTracking.UI.Components;

public static class InventoryTablePresenter {
    public static void DrawTable(
        string tableId,
        IEnumerable<TrackedItem> items,
        ILocalizationService localization,
        IItemResolverService itemResolver,
        ITextureProvider textureProvider) {

        if (!ImGui.BeginTable(tableId, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, -1))) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 24f);
        ImGui.TableSetupColumn(localization.Translate("InventoryTab_ColName"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(localization.Translate("InventoryTab_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 60f);
        ImGui.TableHeadersRow();

        foreach (var item in items.OrderBy(i => i.ContainerId).ThenBy(i => i.SlotIndex)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var iconId = itemResolver.ResolveIconId(item.ItemId);
            if (iconId > 0) {
                var iconWrap = textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();
                if (iconWrap != null) ImGui.Image(iconWrap.Handle, new Vector2(24, 24));
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(itemResolver.ResolveItemName(item.ItemId));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(item.Quantity.ToString("N0"));
        }

        ImGui.EndTable();
    }
}