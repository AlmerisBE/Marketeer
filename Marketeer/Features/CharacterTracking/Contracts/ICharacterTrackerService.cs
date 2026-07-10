using Marketeer.Features.CharacterTracking.Models;
using System;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Contracts;

public interface ICharacterTrackerService {

    event Action<string, uint>? CharacterForgotten;

    IReadOnlyList<TrackedCharacter> GetKnownCharacters();
    void RecordCurrentCharacter();
    void ForgetCharacter(string name, uint homeWorldId);
    bool IsActiveCharacter(string name, uint homeWorldId);
}