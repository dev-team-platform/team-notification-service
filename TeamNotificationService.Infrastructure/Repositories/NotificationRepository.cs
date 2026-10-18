using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Infrastructure.Persistence.Extensions;

namespace TeamNotificationService.Infrastructure.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(Serilog.ILogger logger, AppDbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public async Task<IReadOnlyList<GetLatestNotificationForUserResponseModel>> FindLatestForUserAsync(
        string userIdentitySubject,
        string sourceApp,
        bool isUnreadOnly,
        CancellationToken cancellationToken = default)
    {
        var earliestCreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var userNotifications = from recipient in _dbContext.Set<NotificationRecipient>()
                                join notification in _dbContext.Set<Notification>()
                                    on recipient.NotificationId equals notification.Id
                                where recipient.UserIdentitySubject == userIdentitySubject
                                      && notification.SourceApp == sourceApp
                                      && (!isUnreadOnly || !recipient.IsRead)
                                select new { Recipient = recipient, Notification = notification };

        var queryable = from userNotification in userNotifications
                        where userNotification.Notification.CreatedAt >= earliestCreatedAt
                        orderby userNotification.Notification.CreatedAt descending,
                                userNotification.Notification.Id ascending
                        select new GetLatestNotificationForUserResponseModel
                        {
                            NotificationId = userNotification.Notification.Id,
                            Title = userNotification.Notification.Title,
                            Content = userNotification.Notification.Content,
                            Data = userNotification.Notification.Data,
                            CreatedAt = userNotification.Notification.CreatedAt,
                            IsRead = userNotification.Recipient.IsRead,
                            ReadAt = userNotification.Recipient.ReadAt
                        };

        return await queryable.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<FilterResult<GetAllNotificationsResponseModelItem>> FindAllForUserAsync(
        string userIdentitySubject,
        string sourceApp,
        FilterQuery<GetAllNotificationsResponseModelItem> query,
        CancellationToken cancellationToken = default)
    {
        var queryable = from recipient in _dbContext.Set<NotificationRecipient>()
                        join notification in _dbContext.Set<Notification>()
                            on recipient.NotificationId equals notification.Id
                        where recipient.UserIdentitySubject == userIdentitySubject
                              && notification.SourceApp == sourceApp
                        select new GetAllNotificationsResponseModelItem
                        {
                            NotificationId = notification.Id,
                            Title = notification.Title,
                            Content = notification.Content,
                            Data = notification.Data,
                            CreatedAt = notification.CreatedAt,
                            IsRead = recipient.IsRead,
                            ReadAt = recipient.ReadAt
                        };

        if (query.SortFields.Count == 0)
        {
            query.SortFields.Add(new SortField
            {
                FieldName = "createdAt",
                IsAscending = false
            });
        }

        query.SortFields.Add(new SortField
        {
            FieldName = "notificationId",
            IsAscending = true
        });

        return await queryable.ToFilterResultAsync(query, cancellationToken);
    }
}
