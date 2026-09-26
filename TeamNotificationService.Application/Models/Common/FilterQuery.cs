using System.Linq.Expressions;
using System.Text.Json;
using TeamNotificationService.Application.Enums;
using TeamNotificationService.Domain.Utils;

namespace TeamNotificationService.Application.Models.Common;

public abstract class FilterQuery<TModel> : PaginationQuery where TModel : class
{
    public string? SearchGlobalText { get; init; }
    public FilterGroup? FilterGroup { get; init; }
    public List<FilterCriterion> FilterCriteria { get; init; } = [];
    public List<SortField> SortFields { get; init; } = [];

    public abstract FilterQueryMap<TModel> GetFilterQueryMap();

    public virtual string GetContentHash()
    {
        var json = JsonSerializer.Serialize(this, GetType(), JsonUtils.SerializerOptions);
        return Sha256Utils.ComputeHexHash(json);
    }
}

public class FilterCriterion
{
    public string FieldName { get; set; } = null!;
    public FilterOperator Operator { get; set; }
    public List<string> Values { get; set; } = null!;
    public DateTimeFilterOptions? DateTimeFilterOptions { get; set; }
}

public class DateTimeFilterOptions
{
    public int OffsetMinutes { get; init; }
    public TemporalPartType TemporalPartType { get; init; }
}

public class FilterGroup
{
    public FilterGroupLogic Logic { get; init; }
    public List<FilterGroupNode> Children { get; init; } = [];
}

public class FilterGroupNode
{
    public int? CriterionIndex { get; init; }
    public FilterGroup? Group { get; init; }
}

public class FilterQueryMap<TModel> where TModel : class
{
    private readonly Dictionary<string, FilterQueryMapField<TModel>> fields =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, FilterQueryMapField<TModel>> Fields => fields;

    public FilterQueryMap<TModel> Map<TField>(
        string fieldName,
        Expression<Func<TModel, TField>> selector
    )
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Filter field name cannot be blank.", nameof(fieldName));
        }

        var normalizedFieldName = fieldName.Trim();
        fields[normalizedFieldName] = new FilterQueryMapField<TModel>(
            normalizedFieldName,
            selector,
            typeof(TField)
        );
        return this;
    }

    public bool TryGetField(string fieldName, out FilterQueryMapField<TModel> field)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            field = null!;
            return false;
        }

        return fields.TryGetValue(fieldName.Trim(), out field!);
    }
}

public record FilterQueryMapField<TModel>(
    string FieldName,
    LambdaExpression Selector,
    Type FieldType
) where TModel : class;
