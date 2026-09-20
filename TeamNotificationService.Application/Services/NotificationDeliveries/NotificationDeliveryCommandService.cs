using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Constants;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Application.Services.NotificationDeliveries;

public class NotificationDeliveryCommandService : INotificationDeliveryCommandService
{
    private readonly Serilog.ILogger _logger;
    private readonly INotificationDeliveryRepository _notificationDeliveryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationDeliveryCommandService(
        Serilog.ILogger logger,
        INotificationDeliveryRepository notificationDeliveryRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _notificationDeliveryRepository = notificationDeliveryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpsertNotifcationDeliveryResponseModel> UpsertDeliveryAsync(
        UpsertNotificationDeliveryRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        if (requestModel.Status != DeliveryStatus.Delivered && requestModel.Status != DeliveryStatus.Failed)
        {
            throw new UnprocessableEntityException("Delivery status is invalid.", new Dictionary<string, object>
            {
                ["Status"] = requestModel.Status
            });
        }

        var delivery = await _notificationDeliveryRepository.FindFirstByConditionAsync(
                q => q.Where(item =>
                        item.NotificationRecipientId == requestModel.NotificationRecipientId &&
                        item.Channel == requestModel.Channel),
                trackChanges: true,
                cancellationToken: cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (delivery is null)
        {
            delivery = new NotificationDelivery
            {
                Id = Guid.CreateVersion7(),
                NotificationRecipientId = requestModel.NotificationRecipientId,
                Channel = requestModel.Channel,
                CreatedAt = now
            };
            _notificationDeliveryRepository.Add(delivery);
        }

        delivery.Status = requestModel.Status;
        delivery.Destination = requestModel.Destination ?? delivery.Destination;
        delivery.UpdatedAt = now;

        if (string.Equals(requestModel.Status, DeliveryStatus.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            delivery.SentAt = now;
            delivery.DeliveredAt = now;
            delivery.FailedAt = null;
            delivery.LastError = null;
        }
        else if (string.Equals(requestModel.Status, DeliveryStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            delivery.FailedAt = now;
            delivery.LastError = requestModel.LastError is { Length: > 5000 }
                ? requestModel.LastError[..5000]
                : requestModel.LastError;
            delivery.RetryCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UpsertNotifcationDeliveryResponseModel.FromEntity(delivery);
    }
}