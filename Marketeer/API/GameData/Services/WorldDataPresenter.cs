using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.API.GameData.Contracts;

namespace Marketeer.API.GameData.Services;

public class WorldDataPresenter : IWorldDataPresenter {
    private IDataManager dataManager;

    public WorldDataPresenter(IDataManager dataManager) {
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