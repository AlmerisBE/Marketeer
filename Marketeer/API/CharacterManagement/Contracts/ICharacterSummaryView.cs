using Marketeer.API.CharacterManagement.Models;
using System;

namespace Marketeer.API.CharacterManagement.Contracts;

public interface ICharacterSummaryView {
    void Draw(TrackedCharacter character, Action<ulong> onRetainerSelected);
}