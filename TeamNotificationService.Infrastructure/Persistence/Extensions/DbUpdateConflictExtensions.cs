using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Infrastructure.Persistence.Extensions;

public static class DbUpdateConflictExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgres &&
               postgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    public static ConflictException ToConflictException(this DbUpdateException ex, DbContext context)
    {
        var entry = ex.Entries.FirstOrDefault();
        if (entry is null)
            return new ConflictException("No entry found in DbUpdateException.");

        var entityName = entry.Metadata.ClrType.Name;

        var uniqueIndex = entry.Metadata
            .GetIndexes()
            .FirstOrDefault(i => i.IsUnique);

        if (uniqueIndex is null)
            return new ConflictException("No unique index found.");

        var property = uniqueIndex.Properties.First();
        var value = entry.CurrentValues[property.Name]!;
    
        return new ConflictException(
            "Duplicate value for unique constraint.",
            new Dictionary<string, object?>
            {
                ["entity"] = entityName,
                ["field"] = property.Name,
                ["value"] = value
            }
        );
    }
}