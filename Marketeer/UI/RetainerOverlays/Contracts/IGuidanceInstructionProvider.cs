using Marketeer.UI.Guidance.Models;

namespace Marketeer.UI.RetainerOverlays.Contracts;

public interface IGuidanceInstructionProvider {
    GuidanceInstruction? GetCurrentInstruction();
}