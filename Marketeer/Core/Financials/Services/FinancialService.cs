using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Financials.Contracts;
using Marketeer.API.Financials.Models;
using System.Linq;

namespace Marketeer.Core.Financials.Services;

public class FinancialService : IFinancialService {
    private IConfigurationService configurationService;

    public FinancialService(IConfigurationService configurationService) {
        this.configurationService = configurationService;
    }

    public GlobalFinancialSummary GetFinancialSummary() {
        var config = this.configurationService.GetConfig();
        var summary = new GlobalFinancialSummary();

        foreach (var charRecord in config.FinancialRecords.Values) {
            summary.Characters.Add(charRecord);
            summary.GrandTotalGil += charRecord.TotalGil;
            summary.GrandTotalMarketValue += charRecord.TotalMarketValue;
        }

        // Sort characters alphabetically for consistent UI display
        summary.Characters = summary.Characters.OrderBy(c => c.CharacterName).ToList();

        return summary;
    }
}