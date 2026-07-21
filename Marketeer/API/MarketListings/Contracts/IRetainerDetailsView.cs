using Marketeer.API.CharacterManagement.Models;

namespace Marketeer.API.MarketListings.Contracts;

public interface IRetainerDetailsView {
    void Draw(RetainerDisplayData retainer);
}