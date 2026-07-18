using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.Financials.Contracts;
using Marketeer.Features.Financials.Models;
using System.Linq;

namespace Marketeer.Features.Financials.Services;

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

            foreach (var retainer in charRecord.Retainers.Values) {
                summary.GrandTotalGil += retainer.GilHeld;

                foreach (var listing in retainer.MarketListings.Values) {
                    summary.GrandTotalMarketValue += (ulong)listing.PricePerUnit * listing.Quantity;
                }
            }
        }

        // Sort characters alphabetically for consistent UI display
        summary.Characters = summary.Characters.OrderBy(c => c.CharacterName).ToList();

        return summary;
    }
}