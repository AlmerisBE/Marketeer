using Marketeer.UI.RetainerOverlays.Models;

namespace Marketeer.UI.RetainerOverlays.Contracts;

public interface IGuidanceInstructionProvider {
    GuidanceInstruction? GetCurrentInstruction();
}