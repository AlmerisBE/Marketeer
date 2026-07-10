using Marketeer.Features.CharacterTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Contracts;

public interface ICharacterTrackerService {
    IReadOnlyList<TrackedCharacter> GetKnownCharacters();
    void RecordCurrentCharacter();
}