using Marketeer.Features.CharacterTracking.Models;
using System;

namespace Marketeer.Features.Dashboard.Contracts;

public interface ICharacterSummaryView {
    void Draw(TrackedCharacter character, Action<ulong> onRetainerSelected);
}