using Marketeer.Features.RetainerOrchestration.Models;
using System.Collections.Generic;

namespace Marketeer.Features.RetainerOrchestration.Contracts;

public interface IRetainerOrchestratorService {
    bool IsActive { get; }
    void StartOrchestration(IEnumerable<string> retainerNames, RetainerTargetMenu targetMenu, IRetainerTask task);
    void Abort();
}