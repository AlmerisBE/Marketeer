using FFXIVClientStructs.FFXIV.Client.Game;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketeer.Features.MemoryInterop.Providers;

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
                Name = name
            });
        }

        return retainers;
    }
}