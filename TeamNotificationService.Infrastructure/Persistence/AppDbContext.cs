using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence.Extensions;

namespace TeamNotificationService.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw ex.ToConflictException(this);
        }
    }
}
