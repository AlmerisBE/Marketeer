namespace Marketeer.UI.RetainerOverlays.Models;

public class GuidanceInstruction {
    public GuidanceActionType ActionType { get; set; }
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public uint CurrentPrice { get; set; }
    public uint Quantity { get; set; }
    public uint? TargetPrice { get; set; }
    public string RetainerName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
}