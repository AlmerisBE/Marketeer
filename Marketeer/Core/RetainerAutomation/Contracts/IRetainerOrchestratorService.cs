using Marketeer.Core.RetainerAutomation.Models;
using System.Collections.Generic;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IRetainerOrchestratorService {
    bool IsActive { get; }
    void StartOrchestration(IEnumerable<string> retainerNames, RetainerTargetMenu targetMenu, IRetainerTask task);
    void Abort();
}