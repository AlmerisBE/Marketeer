namespace Marketeer.Features.Retainers.Contracts;

public interface IRetainerService {
    bool IsRetainerAvailable(string retainerName);
    bool SelectRetainer(string retainerName);

    bool IsMenuReadyForRetainer(string retainerName);
    bool IsMenuOptionAvailable(string optionText);
    bool SelectMenuOption(string optionText);

    bool CloseRetainerMenu();
    bool CloseMarketListings();
    bool CloseSalesHistory();
}