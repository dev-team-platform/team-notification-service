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
        var earliestSentAt = DateTimeOffset.UtcNow.AddDays(-10);
        var queryable = BuildLatestForUserQuery(
            _dbContext.Set<NotificationRecipient>(), _dbContext.Set<Notification>(),
            userIdentitySubject, sourceApp, isUnreadOnly, earliestSentAt);

        return await queryable.AsNoTracking().ToListAsync(cancellationToken);
    }

    internal static IQueryable<GetLatestNotificationForUserResponseModel> BuildLatestForUserQuery(
        IQueryable<NotificationRecipient> recipients,
        IQueryable<Notification> notifications,
        string userIdentitySubject,
        string sourceApp,
        bool isUnreadOnly,
        DateTimeOffset earliestSentAt)
    {
        var userNotifications = from recipient in recipients
                                join notification in notifications
                                    on recipient.NotificationId equals notification.Id
                                where recipient.UserIdentitySubject == userIdentitySubject
                                      && notification.SourceApp == sourceApp
                                      && (!isUnreadOnly || !recipient.IsRead)
                                select new
                                {
                                    Recipient = recipient,
                                    Notification = notification,
                                    SentAt = recipient.UpdatedAt ?? recipient.CreatedAt
                                };

        return from userNotification in userNotifications
               where userNotification.SentAt >= earliestSentAt
               orderby userNotification.SentAt descending,
                       userNotification.Notification.Id ascending
               select new GetLatestNotificationForUserResponseModel
               {
                   NotificationId = userNotification.Notification.Id,
                   Title = userNotification.Notification.Title,
                   Content = userNotification.Notification.Content,
                   Data = userNotification.Notification.Data,
                   SentAt = userNotification.SentAt,
                   IsRead = userNotification.Recipient.IsRead,
                   ReadAt = userNotification.Recipient.ReadAt
               };

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
                            SentAt = recipient.UpdatedAt ?? recipient.CreatedAt,
                            IsRead = recipient.IsRead,
                            ReadAt = recipient.ReadAt
                        };

        if (query.SortFields.Count == 0)
        {
            query.SortFields.Add(new SortField
            {
                FieldName = "sentAt",
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
