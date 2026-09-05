using Marketeer.UI.Guidance.Models;

namespace Marketeer.UI.Guidance.Contracts;

public interface IGuidanceInstructionProvider {
    GuidanceInstruction? GetCurrentInstruction();
}