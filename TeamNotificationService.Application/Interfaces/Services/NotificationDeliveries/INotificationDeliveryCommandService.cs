using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;

public interface INotificationDeliveryCommandService
{
    Task<UpsertNotifcationDeliveryResponseModel> UpsertDeliveryAsync(
        UpsertNotificationDeliveryRequestModel requestModel,
        CancellationToken cancellationToken = default);
}