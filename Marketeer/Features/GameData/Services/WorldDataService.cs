using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Features.CharacterTracking.Contracts;

namespace Marketeer.Features.GameData.Services;

public class WorldDataService : IWorldDataService {
    private IDataManager dataManager;

    public WorldDataService(IDataManager dataManager) {
        this.dataManager = dataManager;
    }

    public string GetWorldName(uint worldId) {
        var sheet = this.dataManager.GetExcelSheet<World>();

        if (sheet == null || !sheet.HasRow(worldId)) {
            return worldId.ToString();
        }

        var worldName = sheet.GetRow(worldId).Name.ToString();

        return string.IsNullOrWhiteSpace(worldName) ? worldId.ToString() : worldName;
    }
}