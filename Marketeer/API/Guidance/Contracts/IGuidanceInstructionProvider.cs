using Marketeer.API.Guidance.Models;

namespace Marketeer.API.Guidance.Contracts;

public interface IGuidanceInstructionProvider {
    GuidanceInstruction? GetCurrentInstruction();
}