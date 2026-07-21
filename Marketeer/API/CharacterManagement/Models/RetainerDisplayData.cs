namespace Marketeer.API.CharacterManagement.Models;

public class RetainerDisplayData {
    public ulong RetainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint MarketItemCount { get; set; }
    public uint Gil { get; set; }
}