using Marketeer.Core.CharacterManagement.Models;

namespace Marketeer.Core.MarketListings.Contracts;

public interface IRetainerDetailsView {
    void Draw(RetainerDisplayData retainer);
}