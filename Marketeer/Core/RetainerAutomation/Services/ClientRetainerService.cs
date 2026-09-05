using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.Core.RetainerAutomation.Contracts;

namespace Marketeer.Core.RetainerAutomation.Services;

public unsafe class ClientRetainerService : IClientRetainerService {
    public int GetActiveRetainerCount() {
        var manager = RetainerManager.Instance();
        if (manager == null) {
            return 0;
        }

        int count = 0;
        // The game supports a maximum of 10 retainers per character
        for (uint i = 0; i < 10; i++) {
            var retainer = manager->GetRetainerBySortedIndex(i);
            // If the RetainerId is greater than 0, the slot contains an actively hired retainer
            if (retainer != null && retainer->RetainerId != 0) {
                count++;
            }
        }

        return count;
    }
}