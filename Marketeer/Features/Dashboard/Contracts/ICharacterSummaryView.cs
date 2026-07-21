using Marketeer.Features.CharacterTracking.Models;

namespace Marketeer.Features.Dashboard.Contracts;

public interface ICharacterSummaryView {
    void Draw(TrackedCharacter character);
}