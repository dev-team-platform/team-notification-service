using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Infrastructure.Persistence.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.TemplateKey)
            .HasColumnName("template_key")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SubjectTemplate)
            .HasColumnName("subject_template")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BodyTemplate)
            .HasColumnName("body_template")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(x => x.TemplateKey)
            .HasDatabaseName("uq_email_templates_template_key")
            .IsUnique();
    }
}
