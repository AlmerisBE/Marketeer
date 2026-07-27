using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.RetainerAutomation.Contracts;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerGuidanceService : IRetainerGuidanceService, IGuidanceInstructionProvider {
    private GuidanceInstruction? currentInstruction;

    public void SetInstruction(GuidanceInstruction instruction) => this.currentInstruction = instruction;
    public void ClearInstruction() => this.currentInstruction = null;
    public GuidanceInstruction? GetCurrentInstruction() => this.currentInstruction;
}