using Dalamud.Plugin.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.MarketWatch.Contracts;

namespace Marketeer.API.GameInterop.Services;

public class PlayerContextService : ICompetitionPlayerContext, IMarketWatchPlayerContext {
    private IObjectTable objectTable;

    public PlayerContextService(IObjectTable objectTable) {
        this.objectTable = objectTable;
    }

    public bool IsPlayerAvailable() {
        return this.objectTable.LocalPlayer != null;
    }

    public uint GetCurrentWorldId() {
        return this.objectTable.LocalPlayer?.CurrentWorld.RowId ?? 0;
    }
}