using Marketeer.Core.CharacterManagement.Models;
using System;

namespace Marketeer.Core.CharacterManagement.Contracts;

public interface ICharacterSummaryView {
    void Draw(TrackedCharacter character, Action<ulong> onRetainerSelected);
}