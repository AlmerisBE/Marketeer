using Marketeer.Features.RetainerTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.RetainerTracking.Contracts;

public interface IRetainerTrackerService {
    IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId);
    void RecordRetainers();
}