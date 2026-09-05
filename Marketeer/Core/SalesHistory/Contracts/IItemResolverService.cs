namespace Marketeer.Core.SalesHistory.Contracts;

public interface IItemResolverService {
    uint ResolveItemId(string itemName);
    string ResolveItemName(uint itemId);
    uint ResolveIconId(uint itemId);
    uint ResolveVendorPrice(uint itemId);
}