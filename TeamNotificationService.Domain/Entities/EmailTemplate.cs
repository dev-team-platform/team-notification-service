namespace TeamNotificationService.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; set; }
    public string TemplateKey { get; set; } = null!;
    public string SubjectTemplate { get; set; } = null!;
    public string BodyTemplate { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
