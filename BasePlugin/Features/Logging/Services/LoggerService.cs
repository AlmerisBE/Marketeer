using BasePlugin.Features.Logging.Contracts;
using Dalamud.Plugin.Services;
using System;

namespace BasePlugin.Features.Logging.Services;

public class LoggerService : ILoggerService {
    private IPluginLog pluginLog;

    public LoggerService(IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
    }

    public void Verbose(string message) => this.pluginLog.Verbose(message);

    public void Debug(string message) => this.pluginLog.Debug(message);

    public void Info(string message) => this.pluginLog.Info(message);

    public void Warning(string message) => this.pluginLog.Warning(message);

    public void Error(string message) => this.pluginLog.Error(message);

    public void Error(Exception exception, string message) => this.pluginLog.Error(exception, message);
}