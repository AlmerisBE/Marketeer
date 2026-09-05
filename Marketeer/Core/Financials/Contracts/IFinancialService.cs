using Marketeer.Core.Financials.Models;

namespace Marketeer.Core.Financials.Contracts;

public interface IFinancialService {
    GlobalFinancialSummary GetFinancialSummary();
}