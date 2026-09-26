using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;

namespace TeamNotificationService.Infrastructure.Repositories;

public class EmailTemplateRepository : GenericRepository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(Serilog.ILogger logger, AppDbContext dbContext)
        : base(logger, dbContext)
    {
    }
}
