using Marketeer.Features.Dashboard.Models;

namespace Marketeer.Features.Dashboard.Contracts;

public interface IRetainerDetailsView {
    void Draw(RetainerDisplayData retainer);
}