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

        var notificationIds = requestModel.NotificationIds.Distinct().ToArray();
        if (notificationIds.Length == 0)
        {
            throw new UnprocessableEntityException(
                "At least one notification ID is required when IsAll is false.");
        }

        var notificationRecipients = await _notificationRecipientRepository.FindAllByConditionAsync(
            query => query.Where(recipient =>
                notificationIds.Contains(recipient.NotificationId)
                && recipient.UserIdentitySubject == identitySubject),
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (notificationRecipients.Count != notificationIds.Length)
        {
            throw new NotFoundException("Notification was not found.");
        }

        var markedCount = 0;
        foreach (var notificationRecipient in notificationRecipients)
        {
            if (notificationRecipient.IsRead)
            {
                continue;
            }

            notificationRecipient.IsRead = true;
            notificationRecipient.ReadAt = now;
            markedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new MarkNotificationReadResponseModel
        {
            MarkedCount = markedCount
        };
    }
}
