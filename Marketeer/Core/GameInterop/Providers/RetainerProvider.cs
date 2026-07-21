using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.GameInterop.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketeer.Core.GameInterop.Providers;

public unsafe class RetainerProvider : IRetainerProvider {
    public IReadOnlyList<TrackedRetainer> GetActiveRetainers() {
        var retainers = new List<TrackedRetainer>();
        var manager = RetainerManager.Instance();

        if (manager == null) {
            return retainers;
        }

        for (uint i = 0; i < 10; i++) {
            var retainer = manager->GetRetainerBySortedIndex(i);

            if (retainer == null || retainer->RetainerId == 0) {
                continue;
            }

            var nameSpan = retainer->Name;

            int nullIndex = nameSpan.IndexOf((byte)0);
            if (nullIndex >= 0) {
                nameSpan = nameSpan.Slice(0, nullIndex);
            }

            var name = Encoding.UTF8.GetString(nameSpan);

            retainers.Add(new TrackedRetainer {
                RetainerId = retainer->RetainerId,
                Name = name,
                MarketItemCount = retainer->MarketItemCount,
                Gil = retainer->Gil
            });
        }

        return retainers;
    }
}