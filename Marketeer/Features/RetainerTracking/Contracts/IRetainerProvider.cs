using Marketeer.Features.RetainerTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.RetainerTracking.Contracts;

public interface IRetainerProvider {
    // Reads active retainers from the game's unmanaged memory
    IReadOnlyList<TrackedRetainer> GetActiveRetainers();
}