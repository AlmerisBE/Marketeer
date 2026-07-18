using Marketeer.Features.Financials.Models;

namespace Marketeer.Features.Financials.Contracts;

public interface IFinancialService {
    GlobalFinancialSummary GetFinancialSummary();
}