using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Marketeer.API.GameInterop.Contracts;
using System;
using System.Linq;
using System.Numerics;

namespace Marketeer.API.GameInterop.Services;

public unsafe class WorldInteractionService : IWorldInteractionService {
    private IObjectTable objectTable;

    // We check against the localized names of the Summoning Bell in all 4 supported languages
    private readonly string[] bellNames = { "Summoning Bell", "Sonnette", "Krämerklingel", "リテイナーベル" };

    public WorldInteractionService(IObjectTable objectTable) {
        this.objectTable = objectTable;
    }

    public bool InteractWithSummoningBell() {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) {
            return false;
        }

        var playerPos = localPlayer.Position;

        var bell = this.objectTable
            .Where(obj => obj.IsTargetable && this.bellNames.Any(name => obj.Name.TextValue.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(obj => Vector3.Distance(playerPos, obj.Position))
            .FirstOrDefault();

        if (bell == null) {
            return false;
        }

        var distance = Vector3.Distance(playerPos, bell.Position);
        if (distance > 6.0f) {
            return false;
        }

        var targetSystem = TargetSystem.Instance();
        if (targetSystem == null) {
            return false;
        }

        // Directly invoke the native interaction function in FFXIV memory
        targetSystem->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)bell.Address);
        return true;
    }
}