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

    public async Task<IReadOnlyList<GetUnreadNotificationsResponseModel>> FindUnreadForUserAsync(
        string userIdentitySubject,
        CancellationToken cancellationToken = default)
    {
        var earliestCreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var queryable = from recipient in _dbContext.Set<NotificationRecipient>()
                        join notification in _dbContext.Set<Notification>()
                            on recipient.NotificationId equals notification.Id
                        where recipient.UserIdentitySubject == userIdentitySubject
                            && !recipient.IsRead
                            && notification.CreatedAt >= earliestCreatedAt
                        orderby notification.CreatedAt descending,
                                notification.Id ascending
                        select new GetUnreadNotificationsResponseModel
                        {
                            NotificationId = notification.Id,
                            Title = notification.Title,
                            Content = notification.Content,
                            Data = notification.Data,
                            CreatedAt = notification.CreatedAt
                        };

        return await queryable.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<FilterResult<GetAllNotificationsResponseModelItem>> FindAllForUserAsync(
        string userIdentitySubject,
        FilterQuery<GetAllNotificationsResponseModelItem> query,
        CancellationToken cancellationToken = default)
    {
        var queryable = from recipient in _dbContext.Set<NotificationRecipient>()
                        join notification in _dbContext.Set<Notification>()
                            on recipient.NotificationId equals notification.Id
                        where recipient.UserIdentitySubject == userIdentitySubject
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
