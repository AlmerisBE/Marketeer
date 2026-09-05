using Marketeer.UI.Guidance.Models;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerGuidanceService {
    void SetInstruction(GuidanceInstruction instruction);
    void ClearInstruction();
}