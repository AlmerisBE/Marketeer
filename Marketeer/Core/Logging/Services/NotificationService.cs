using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Marketeer.Core.Logging.Contracts;

namespace Marketeer.Core.Logging.Services;

public class NotificationService : INotificationService {
    private INotificationManager notificationManager;

    public NotificationService(INotificationManager notificationManager) {
        this.notificationManager = notificationManager;
    }

    public void ShowSuccess(string title, string message) {
        var notification = new Notification {
            Title = title,
            Content = message,
            Type = NotificationType.Success
        };
        this.notificationManager.AddNotification(notification);
    }

    public void ShowWarning(string title, string message) {
        var notification = new Notification {
            Title = title,
            Content = message,
            Type = NotificationType.Warning
        };
        this.notificationManager.AddNotification(notification);
    }

    public void ShowError(string title, string message) {
        var notification = new Notification {
            Title = title,
            Content = message,
            Type = NotificationType.Error
        };
        this.notificationManager.AddNotification(notification);
    }
}