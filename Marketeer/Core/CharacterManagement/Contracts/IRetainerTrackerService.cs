using Marketeer.Core.CharacterManagement.Models;
using System.Collections.Generic;

namespace Marketeer.Core.CharacterManagement.Contracts;

public interface IRetainerTrackerService {
    IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId);
    void RecordRetainers();
}