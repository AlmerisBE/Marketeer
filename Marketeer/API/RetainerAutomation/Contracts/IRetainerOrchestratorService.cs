using Marketeer.API.RetainerAutomation.Models;
using System.Collections.Generic;

namespace Marketeer.API.RetainerAutomation.Contracts;

public interface IRetainerOrchestratorService {
    bool IsActive { get; }
    void StartOrchestration(IEnumerable<string> retainerNames, RetainerTargetMenu targetMenu, IRetainerTask task);
    void Abort();
}