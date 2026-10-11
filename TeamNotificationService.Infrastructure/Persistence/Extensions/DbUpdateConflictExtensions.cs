using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
        if (ex.InnerException is not PostgresException postgres ||
            string.IsNullOrWhiteSpace(postgres.ConstraintName))
        {
            return new ConflictException("Duplicate value for unique constraint.");
        }

        var match = ex.Entries
            .Where(entry => string.IsNullOrWhiteSpace(postgres.TableName) ||
                            string.Equals(
                                entry.Metadata.GetTableName(),
                                postgres.TableName,
                                StringComparison.OrdinalIgnoreCase))
            .Select(entry => new
            {
                Entry = entry,
                Index = FindViolatedUniqueIndex(entry.Metadata, postgres.ConstraintName)
            })
            .FirstOrDefault(candidate => candidate.Index is not null);

        if (match?.Index is null)
            return new ConflictException("Duplicate value for unique constraint.");

        var properties = match.Index.Properties;
        var field = string.Join(", ", properties.Select(property => property.Name));
        var propertyValues = properties
            .Select(property => match.Entry.CurrentValues[property.Name])
            .ToArray();
        object? value = propertyValues.Length == 1
            ? propertyValues[0]
            : string.Join(", ", propertyValues);

        return new ConflictException(
            "Duplicate value for unique constraint.",
            new Dictionary<string, object?>
            {
                ["entity"] = match.Entry.Metadata.ClrType.Name,
                ["field"] = field,
                ["value"] = value
            }
        );
    }

    private static IIndex? FindViolatedUniqueIndex(IEntityType entityType, string constraintName)
    {
        var tableName = entityType.GetTableName();
        if (tableName is null)
            return null;

        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());

        return entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .FirstOrDefault(index => IsConstraintNameMatch(
                index,
                tableName,
                storeObject,
                constraintName));
    }

    private static bool IsConstraintNameMatch(
        IIndex index,
        string tableName,
        StoreObjectIdentifier storeObject,
        string constraintName)
    {
        if (NamesMatch(index.GetDatabaseName(), constraintName))
            return true;

        var columnNames = index.Properties
            .Select(property => property.GetColumnName(storeObject))
            .ToArray();

        if (columnNames.Any(string.IsNullOrWhiteSpace))
            return false;

        // Some SQL DDL names unique constraints as uq_<table>_<column(s)>.
        var configuredDdlName = $"uq_{tableName}_{string.Join("_", columnNames)}";
        return NamesMatch(configuredDdlName, constraintName);
    }

    private static bool NamesMatch(string? expectedName, string actualName)
    {
        return !string.IsNullOrWhiteSpace(expectedName) &&
               string.Equals(
                   NormalizePostgresIdentifier(expectedName),
                   NormalizePostgresIdentifier(actualName),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePostgresIdentifier(string name)
    {
        // Current schema identifiers are ASCII, so this mirrors PostgreSQL's 63-byte limit.
        return name.Length <= 63 ? name : name[..63];
    }
}
