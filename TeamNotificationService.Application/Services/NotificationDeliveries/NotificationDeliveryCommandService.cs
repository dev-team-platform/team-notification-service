using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Enums;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;

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
        var parsedStatus = EnumUtils.Parse<DeliveryStatus>(requestModel.Status);
        var parsedChannel = EnumUtils.Parse<ChannelType>(requestModel.Channel);

        var delivery = await _notificationDeliveryRepository.FindFirstByConditionAsync(
                q => q.Where(item =>
                        item.NotificationRecipientId == requestModel.NotificationRecipientId &&
                        item.Channel == parsedChannel),
                trackChanges: true,
                cancellationToken: cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (delivery is null)
        {
            delivery = new NotificationDelivery
            {
                Id = Guid.CreateVersion7(),
                NotificationRecipientId = requestModel.NotificationRecipientId,
                Channel = parsedChannel,
                CreatedAt = now
            };
            _notificationDeliveryRepository.Add(delivery);
        }

        delivery.Status = parsedStatus;
        delivery.Destination = requestModel.Destination ?? delivery.Destination;
        delivery.UpdatedAt = now;

        if (parsedStatus == DeliveryStatus.Delivered)
        {
            delivery.SentAt = now;
            delivery.DeliveredAt = now;
            delivery.FailedAt = null;
            delivery.LastError = null;
        }
        else if (parsedStatus == DeliveryStatus.Failed)
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