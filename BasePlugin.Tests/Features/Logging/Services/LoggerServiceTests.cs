using BasePlugin.Features.Logging.Services;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace BasePlugin.Tests.Features.Logging.Services;

public class LoggerServiceTests {
    [Fact]
    public void LoggerService_Verbose_CallsPluginLogVerbose() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var message = "Test verbose message";

        // Act
        logger.Verbose(message);

        // Assert
        mockPluginLog.Received(1).Verbose(message);
    }

    [Fact]
    public void LoggerService_Debug_CallsPluginLogDebug() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var message = "Test debug message";

        // Act
        logger.Debug(message);

        // Assert
        mockPluginLog.Received(1).Debug(message);
    }

    [Fact]
    public void LoggerService_Info_CallsPluginLogInfo() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var message = "Test info message";

        // Act
        logger.Info(message);

        // Assert
        mockPluginLog.Received(1).Info(message);
    }

    [Fact]
    public void LoggerService_Warning_CallsPluginLogWarning() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var message = "Test warning message";

        // Act
        logger.Warning(message);

        // Assert
        mockPluginLog.Received(1).Warning(message);
    }

    [Fact]
    public void LoggerService_Error_CallsPluginLogError() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var message = "Test error message";

        // Act
        logger.Error(message);

        // Assert
        mockPluginLog.Received(1).Error(message);
    }

    [Fact]
    public void LoggerService_ErrorWithException_CallsPluginLogErrorWithException() {
        // Arrange
        var mockPluginLog = Substitute.For<IPluginLog>();
        var logger = new LoggerService(mockPluginLog);
        var exception = new InvalidOperationException("Test exception");
        var message = "Test error message with exception";

        // Act
        logger.Error(exception, message);

        // Assert
        mockPluginLog.Received(1).Error(exception, message);
    }
}