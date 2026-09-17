using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.UI.Guidance.Models;
using Marketeer.UI.RetainerOverlays.Contracts;

namespace Marketeer.Core.RetainerAutomation.Services;

public class RetainerGuidanceService : IRetainerGuidanceService, IGuidanceInstructionProvider {
    private GuidanceInstruction? currentInstruction;

    public void SetInstruction(GuidanceInstruction instruction) => this.currentInstruction = instruction;
    public void ClearInstruction() => this.currentInstruction = null;
    public GuidanceInstruction? GetCurrentInstruction() => this.currentInstruction;
}