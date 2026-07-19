namespace Marketeer.Features.SalesHistoryTracking.Contracts;

public interface IItemResolverService {
    uint ResolveItemId(string itemName);
}