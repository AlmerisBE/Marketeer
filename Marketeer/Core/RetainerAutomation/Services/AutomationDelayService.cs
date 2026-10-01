using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;

namespace Marketeer.Core.RetainerAutomation.Services;

public class AutomationDelayService : IAutomationDelayProvider {
    private IConfigurationService configService;
    private Random random;

    public AutomationDelayService(IConfigurationService configService) {
        this.configService = configService;
        this.random = new Random();
    }

    public int GetDelayMs(int baseTechnicalDelayMs) {
        var config = this.configService.GetConfig();

        // If humanized delays are disabled, we MUST still return the technical minimum to prevent UI race conditions
        if (!config.EnableAutomationDelay) return baseTechnicalDelayMs;

        int minMs = config.AutomationDelayMin * 1000;
        int maxMs = config.AutomationDelayMax * 1000;
        if (minMs > maxMs) minMs = maxMs;

        int humanDelay = this.random.Next(minMs, maxMs + 1);

        return baseTechnicalDelayMs + humanDelay;
    }

    public TimeSpan GetDelay(int baseTechnicalDelayMs) {
        return TimeSpan.FromMilliseconds(this.GetDelayMs(baseTechnicalDelayMs));
    }
}