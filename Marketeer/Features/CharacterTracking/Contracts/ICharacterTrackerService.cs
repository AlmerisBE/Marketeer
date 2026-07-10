using Marketeer.Features.CharacterTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Contracts;

public interface ICharacterTrackerService {
    IReadOnlyList<TrackedCharacter> GetKnownCharacters();
    void RecordCurrentCharacter();
    void ForgetCharacter(string name, uint homeWorldId);
    bool IsActiveCharacter(string name, uint homeWorldId);
}