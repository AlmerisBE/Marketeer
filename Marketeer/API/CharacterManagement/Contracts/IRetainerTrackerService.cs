using Marketeer.API.CharacterManagement.Models;
using System.Collections.Generic;

namespace Marketeer.API.CharacterManagement.Contracts;

public interface IRetainerTrackerService {
    IReadOnlyList<TrackedRetainer> GetRetainersForCharacter(string characterName, uint homeWorldId);
    void RecordRetainers();
}