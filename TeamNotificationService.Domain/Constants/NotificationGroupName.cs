namespace TeamNotificationService.Domain.Constants;

public static class NotificationGroupName
{
    public static string ForUser(string sourceApp, string identitySubject) =>
        $"{sourceApp}:{identitySubject}";
}
