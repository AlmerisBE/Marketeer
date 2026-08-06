using System.Collections.Generic;

namespace Marketeer.API.CraftingProfit.Models;

public class ComponentConfig {
    public uint ItemId { get; set; }
    public uint TargetBuyPrice { get; set; }
    public bool CraftRecursively { get; set; }
}

public class CraftingItemConfig {
    public uint ItemId { get; set; }
    public uint TargetSellPrice { get; set; }
    public Dictionary<uint, ComponentConfig> Components { get; set; } = new();
}

public class ComponentEvaluation {
    public uint ItemId { get; set; }
    public uint QuantityRequired { get; set; }
    public bool IsCraftedRecursively { get; set; }
    public uint UnitCost { get; set; }
    public uint TotalCost => this.UnitCost * this.QuantityRequired;
    public List<ComponentEvaluation> SubComponents { get; set; } = new();
}

public class CraftingProfitResult {
    public uint ItemId { get; set; }
    public uint CurrentMarketPrice { get; set; }
    public uint TargetSellPrice { get; set; }
    public uint TotalCraftingCost { get; set; }
    public List<ComponentEvaluation> ComponentEvaluations { get; set; } = new();

    public int Profit => (int)this.CurrentMarketPrice - (int)this.TotalCraftingCost;
}