using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(255);

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasColumnType("text");

        builder.Property(x => x.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedById)
            .HasColumnName("created_by_id")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_notifications_created_at");

        builder.HasIndex(x => x.CreatedById)
            .HasDatabaseName("ix_notifications_created_by_id");
    }
}
