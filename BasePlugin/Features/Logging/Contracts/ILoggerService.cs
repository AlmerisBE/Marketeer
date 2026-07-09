using System;

namespace BasePlugin.Features.Logging.Contracts;

public interface ILoggerService {
    void Verbose(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(Exception exception, string message);
}