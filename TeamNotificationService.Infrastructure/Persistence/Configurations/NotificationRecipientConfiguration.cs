using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Infrastructure.Persistence.Configurations;

public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("notification_recipients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.NotificationId)
            .HasColumnName("notification_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.UserIdentitySubject)
            .HasColumnName("user_identity_subject")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IsRead)
            .HasColumnName("is_read")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ReadAt)
            .HasColumnName("read_at");

        builder.Property(x => x.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("archived_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Notification)
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_recipients_notification_id");

        builder.HasIndex(x => new { x.UserId, x.NotificationId })
            .HasDatabaseName("uq_notification_recipients_user_id_notification_id")
            .IsUnique();

        builder.HasIndex(x => new { x.UserIdentitySubject, x.NotificationId })
            .HasDatabaseName("uq_notification_recipients_user_identity_subject_notification_id")
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("ix_notification_recipients_user_id_created_at");

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("ix_notification_recipients_user_id_is_read_created_at");
    }
}
