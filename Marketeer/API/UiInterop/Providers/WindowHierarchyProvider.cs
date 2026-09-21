using Marketeer.API.UiInterop.Contracts;
using System.Collections.Generic;

namespace Marketeer.API.UiInterop.Providers;

public class WindowHierarchyProvider : IWindowHierarchyProvider {
    private Dictionary<string, string> hierarchyMap;

    public WindowHierarchyProvider() {
        // Map known child windows to their logical parent window frames
        this.hierarchyMap = new Dictionary<string, string> {
            // Social & Communication
            { "PartyMemberList", "Social" },
            { "FriendList", "Social" },
            { "SocialList", "Social" },

            { "ContactList", "Social" },
            { "BlackList", "Social" },
            { "CrossRealmTeam", "Social" },
            { "FreeCompanyMember", "FreeCompany" },
            { "FreeCompanyInfo", "FreeCompany" },
            { "LinkShell", "Social" },
            { "CrossWorldLinkshell", "Social" },

            // Character & Inventory
            { "CharacterStatus", "Character" },
            { "ArmouryBoard", "Character" },
            { "Currency", "Character" },
            { "InventoryGrid0E", "InventoryExpansion" },
            { "InventoryGrid1E", "InventoryExpansion" },
            { "InventoryGrid2E", "InventoryExpansion" },
            { "InventoryGrid3E", "InventoryExpansion" },
            { "InventoryLarge", "Inventory" },
            { "InventoryExpansion", "Inventory" },

            // Retainers & Bank
            { "RetainerList", "Bank" },
            { "RetainerGrid0", "Retainer" },
            { "RetainerGrid1", "Retainer" },
            { "RetainerGrid2", "Retainer" },

            // Crafting & Gathering
            { "RecipeTree", "RecipeNote" },
            { "RecipeMaterialList", "RecipeNote" },
            { "GatheringItem", "GatheringNote" },

            // Duties & Quests
            { "ContentsFinderBoard", "ContentsFinder" },
            { "JournalDetail", "Journal" },

            // Shops & Exchanges
            { "GrandCompanyExchange", "GrandCompany" },
            { "ShopExchangeItem", "Shop" },
            { "ShopExchangeCurrency", "Shop" }
        };
    }

    public string? GetParentWindowName(string childWindowName) {
        if (this.hierarchyMap.TryGetValue(childWindowName, out var parentName)) {
            return parentName;
        }
        return null;
    }
}