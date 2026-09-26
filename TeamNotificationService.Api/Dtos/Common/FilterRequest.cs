using System.Linq.Expressions;
using TeamNotificationService.Api.Utils;
using TeamNotificationService.Application.Enums;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;

namespace TeamNotificationService.Api.Dtos.Common;

public abstract class FilterRequest<TModel> : PaginationRequest where TModel : class
{
    public string? SearchGlobalText { get; init; }
    public string? GroupPattern { get; init; }
    public List<FilterCriterionRequest> FilterCriteria { get; init; } = [];
    public List<SortFieldRequest> SortFields { get; init; } = [];

    public abstract FilterQueryMapRequest<TModel> GetFilterRequestMap();

    public FilterGroup? BuildFilterGroup()
    {
        return FilterGroupConverter.Build(GroupPattern, FilterCriteria?.Count ?? 0);
    }

    public List<FilterCriterion> BuildFilterCriteria()
    {
        if (FilterCriteria.Count == 0)
        {
            return [];
        }

        var filterRequestMap = GetFilterRequestMap();

        return [..FilterCriteria
            .Select(criterion =>
            {
                if (!filterRequestMap.TryGetField(criterion.FieldName, out _))
                {
                    throw new UnprocessableEntityException(
                        "Unknown filter field.",
                        new Dictionary<string, object?>
                        {
                            ["fieldName"] = criterion.FieldName
                        }
                    );
                }

                return FilterCriterionRequest.ToModel(criterion);
            })];
    }

    public List<SortField> BuildSortFields()
    {
        if (SortFields.Count == 0)
        {
            return [];
        }

        var filterRequestMap = GetFilterRequestMap();

        return [..SortFields
            .Select(sortField =>
            {
                if (!filterRequestMap.TryGetField(sortField.FieldName, out _))
                {
                    throw new UnprocessableEntityException(
                        "Unknown sort field.",
                        new Dictionary<string, object?>
                        {
                            ["fieldName"] = sortField.FieldName
                        }
                    );
                }

                return new SortField
                {
                    FieldName = sortField.FieldName,
                    IsAscending = sortField.IsAscending
                };
            })];
    }
}

public class FilterCriterionRequest
{
    public string FieldName { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public List<string> Values { get; set; } = null!;
    public DateTimeFilterOptionsRequest? DateTimeFilterOptions { get; set; }

    public static FilterCriterion ToModel(FilterCriterionRequest request)
    {
        return new FilterCriterion
        {
            FieldName = request.FieldName,
            Operator = EnumUtils.Parse<FilterOperator>(request.Operator),
            Values = request.Values,
            DateTimeFilterOptions = request.DateTimeFilterOptions is null
                ? null
                : DateTimeFilterOptionsRequest.ToModel(request.DateTimeFilterOptions)
        };
    }
}

public class DateTimeFilterOptionsRequest
{
    public int OffsetMinutes { get; init; }
    public string TemporalPartType { get; init; } = null!;

    public static DateTimeFilterOptions ToModel(DateTimeFilterOptionsRequest request)
    {
        return new DateTimeFilterOptions
        {
            OffsetMinutes = request.OffsetMinutes,
            TemporalPartType = EnumUtils.Parse<TemporalPartType>(request.TemporalPartType)
        };
    }
}

public sealed class FilterQueryMapRequest<TModel> where TModel : class
{
    private readonly Dictionary<string, FilterQueryMapFieldRequest<TModel>> fields = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, FilterQueryMapFieldRequest<TModel>> Fields => fields;

    public FilterQueryMapRequest<TModel> Map<TField>(
        string fieldName,
        Expression<Func<TModel, TField>> selector
    )
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new UnprocessableEntityException(
                "Filter field name cannot be blank.",
                new Dictionary<string, object?>
                {
                    ["fieldName"] = nameof(fieldName)
                });
        }

        var normalizedFieldName = fieldName.Trim();
        fields[normalizedFieldName] = new FilterQueryMapFieldRequest<TModel>(
            normalizedFieldName,
            selector,
            typeof(TField)
        );
        return this;
    }

    public bool TryGetField(string fieldName, out FilterQueryMapFieldRequest<TModel> field)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            field = null!;
            return false;
        }

        return fields.TryGetValue(fieldName.Trim(), out field!);
    }
}

public sealed record FilterQueryMapFieldRequest<TModel>(
    string FieldName,
    LambdaExpression Selector,
    Type FieldType
) where TModel : class;
