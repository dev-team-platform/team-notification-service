using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Infrastructure.Persistence.Configurations;

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.NotificationRecipientId)
            .HasColumnName("notification_recipient_id")
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Destination)
            .HasColumnName("destination")
            .HasMaxLength(255);

        builder.Property(x => x.TemplateKey)
            .HasColumnName("template_key")
            .HasMaxLength(150);

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");

        builder.Property(x => x.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at");

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(x => x.NotificationRecipient)
            .WithMany()
            .HasForeignKey(x => x.NotificationRecipientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_deliveries_notification_recipient_id");

        builder.HasIndex(x => x.NotificationRecipientId)
            .HasDatabaseName("ix_notification_deliveries_recipient_id");

        builder.HasIndex(x => new { x.NotificationRecipientId, x.Channel })
            .HasDatabaseName("uq_notification_deliveries_recipient_channel")
            .IsUnique();

    }
}
