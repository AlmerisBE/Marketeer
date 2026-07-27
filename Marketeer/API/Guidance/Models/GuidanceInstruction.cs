namespace Marketeer.API.Guidance.Models;

public class GuidanceInstruction {
    public GuidanceActionType ActionType { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public uint? TargetPrice { get; set; }
}