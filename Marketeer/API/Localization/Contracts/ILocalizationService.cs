namespace Marketeer.API.Localization.Contracts;

public interface ILocalizationService {
    string Translate(string key);
    string Translate(string key, params object[] args);
}