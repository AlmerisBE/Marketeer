using System;

namespace Marketeer.Core.RetainerAutomation.Contracts;

public interface IAutomationDelayProvider {
    int GetDelayMs(int baseTechnicalDelayMs);
    TimeSpan GetDelay(int baseTechnicalDelayMs);
}