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
    public uint TargetUnitCost { get; set; }
    public uint TotalCost => this.UnitCost * this.QuantityRequired;
    public uint TotalTargetCost => (this.TargetUnitCost > 0 ? this.TargetUnitCost : this.UnitCost) * this.QuantityRequired;
    public List<ComponentEvaluation> SubComponents { get; set; } = new();
}

public class CraftingProfitResult {
    public uint ItemId { get; set; }
    public uint ResultQuantity { get; set; } = 1;
    public uint CurrentMarketPrice { get; set; }
    public uint TargetSellPrice { get; set; }

    public uint BatchCraftingCost { get; set; }
    public uint TotalCraftingCost { get; set; }

    public uint BatchTargetCraftingCost { get; set; }
    public uint TotalTargetCraftingCost { get; set; }

    public List<ComponentEvaluation> ComponentEvaluations { get; set; } = new();

    public int Profit => (int)this.CurrentMarketPrice - (int)this.TotalCraftingCost;
    public int TargetProfit => (int)(this.TargetSellPrice > 0 ? this.TargetSellPrice : this.CurrentMarketPrice) - (int)this.TotalTargetCraftingCost;
}