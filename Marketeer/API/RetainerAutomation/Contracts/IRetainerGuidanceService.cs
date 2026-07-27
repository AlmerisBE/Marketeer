using Marketeer.API.Guidance.Models;

namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IRetainerGuidanceService {
    void SetInstruction(GuidanceInstruction instruction);
    void ClearInstruction();
}