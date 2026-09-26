using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Application.Services.Notifications;

public class NotificationCommandService : INotificationCommandService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly INotificationRecipientRepository _notificationRecipientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationCommandService(
        ICurrentUserContext currentUserContext,
        INotificationRecipientRepository notificationRecipientRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserContext = currentUserContext;
        _notificationRecipientRepository = notificationRecipientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkNotificationReadResponseModel> MarkNotificationReadAsync(
        MarkNotificationReadRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var identitySubject = _currentUserContext.IdentitySubject;
        var now = DateTimeOffset.UtcNow;

        if (requestModel.IsAll)
        {
            var unreadRecipients = await _notificationRecipientRepository.FindAllByConditionAsync(
                query => query.Where(recipient =>
                    recipient.UserIdentitySubject == identitySubject && !recipient.IsRead),
                trackChanges: true,
                cancellationToken: cancellationToken);

            foreach (var recipient in unreadRecipients)
            {
                recipient.IsRead = true;
                recipient.ReadAt = now;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new MarkNotificationReadResponseModel
            {
                MarkedCount = unreadRecipients.Count
            };
        }

        if (requestModel.NotificationId is null)
        {
            throw new UnprocessableEntityException(
                "NotificationId is required when IsAll is false.");
        }

        var notificationRecipient = await _notificationRecipientRepository.FindFirstByConditionAsync(
            query => query.Where(recipient =>
                recipient.NotificationId == requestModel.NotificationId.Value
                && recipient.UserIdentitySubject == identitySubject),
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (notificationRecipient is null)
        {
            throw new NotFoundException("Notification was not found.");
        }

        var markedCount = 0;
        if (!notificationRecipient.IsRead)
        {
            notificationRecipient.IsRead = true;
            notificationRecipient.ReadAt = now;
            markedCount = 1;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new MarkNotificationReadResponseModel
        {
            MarkedCount = markedCount
        };
    }
}
