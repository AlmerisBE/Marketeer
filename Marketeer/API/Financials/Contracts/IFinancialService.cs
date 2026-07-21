using Marketeer.API.Financials.Models;

namespace Marketeer.API.Financials.Contracts;

public interface IFinancialService {
    GlobalFinancialSummary GetFinancialSummary();
}