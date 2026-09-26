using System.Globalization;
using System.Linq.Expressions;
using TeamNotificationService.Application.Enums;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using Microsoft.EntityFrameworkCore;

namespace TeamNotificationService.Infrastructure.Persistence.Extensions;

public static class FilterExtensions
{
    public static IQueryable<TModel> ApplyFiltering<TModel>(
        this IQueryable<TModel> query,
        FilterQuery<TModel> filterQuery
    ) where TModel : class
    {
        return FilterExpression.ApplyFiltering(query, filterQuery);
    }

    public static IQueryable<TModel> ApplySorting<TModel>(
        this IQueryable<TModel> query,
        FilterQuery<TModel> filterQuery
    ) where TModel : class
    {
        return SortingExpression.ApplySorting(query, filterQuery);
    }

    public static IQueryable<TModel> ApplyPaging<TModel>(
        this IQueryable<TModel> query,
        FilterQuery<TModel> filterQuery
    ) where TModel : class
    {
        var currentPage = filterQuery.CurrentPage;
        var itemsPerPage = filterQuery.ItemsPerPage;

        return query
            .Skip((currentPage - 1) * itemsPerPage)
            .Take(itemsPerPage);
    }

    // Runs the pipeline without projection and returns the original entity type.
    public static Task<FilterResult<TModel>> ToFilterResultAsync<TModel>(
        this IQueryable<TModel> query,
        FilterQuery<TModel> filterQuery,
        CancellationToken cancellationToken = default
    ) where TModel : class
    {
        return query.ToFilterResultAsync(filterQuery, static entity => entity, cancellationToken);
    }

    // Runs filtering, sorting, pagination, and projection in one reusable entry point.
    public static async Task<FilterResult<TResult>> ToFilterResultAsync<TModel, TResult>(
        this IQueryable<TModel> query,
        FilterQuery<TModel> filterQuery,
        Expression<Func<TModel, TResult>> selector,
        CancellationToken cancellationToken = default
    )
        where TModel : class
        where TResult : class
    {
        var mappedQuery = query.ApplyFiltering(filterQuery);
        mappedQuery = mappedQuery.ApplySorting(filterQuery);

        return await PaginationExpression.ToFilterResultAsync(
            mappedQuery,
            filterQuery,
            selector,
            cancellationToken
        );
    }

    private static class FilterExpression
    {
        // Converts the request filter payload into one LINQ Where clause.
        public static IQueryable<TModel> ApplyFiltering<TModel>(
            IQueryable<TModel> query,
            FilterQuery<TModel> filterQuery
        ) where TModel : class
        {
            var criteria = filterQuery.FilterCriteria ?? [];
            var searchGlobalText = filterQuery.SearchGlobalText?.Trim();
            var hasGlobalSearch = !string.IsNullOrWhiteSpace(searchGlobalText);

            if (criteria.Count == 0 && !hasGlobalSearch)
            {
                return query;
            }

            var filterQueryMap = filterQuery.GetFilterQueryMap();
            var predicates = new List<Expression<Func<TModel, bool>>>();

            if (criteria.Count > 0)
            {
                var criterionExpressions = criteria
                    .Select(criterion => BuildCriterionExpression(criterion, filterQueryMap))
                    .ToList();

                predicates.Add(BuildGroupExpression(
                    GetEffectiveFilterGroup(filterQuery.FilterGroup, criterionExpressions.Count),
                    criterionExpressions
                ));
            }

            if (hasGlobalSearch)
            {
                predicates.Add(BuildGlobalSearchExpression(filterQueryMap, searchGlobalText!));
            }

            var predicate = CombineExpressions(predicates, Expression.AndAlso);
            return query.Where(predicate);
        }

        // Applies a prefix search across every mapped string field; individual field matches are ORed together.
        private static Expression<Func<TModel, bool>> BuildGlobalSearchExpression<TModel>(
            FilterQueryMap<TModel> filterQueryMap,
            string searchGlobalText
        ) where TModel : class
        {
            var parameter = Expression.Parameter(typeof(TModel), "entity");
            var fieldExpressions = filterQueryMap.Fields.Values
                .Where(field => field.FieldType == typeof(string))
                .Select(field =>
                {
                    var selectorBody = ReplaceParameter(
                        field.Selector.Parameters[0],
                        parameter,
                        field.Selector.Body
                    );
                    var startsWithExpression = BuildStringOperation(
                        selectorBody,
                        nameof(string.StartsWith),
                        searchGlobalText
                    );
                    var nullGuard = GetNullGuard(selectorBody);

                    return nullGuard is null
                        ? startsWithExpression
                        : Expression.AndAlso(nullGuard, startsWithExpression);
                })
                .ToList();

            if (fieldExpressions.Count == 0)
            {
                return _ => false;
            }

            var body = fieldExpressions.Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<TModel, bool>>(body, parameter);
        }

        // Uses the tree produced by FilterGroupConverter when present, otherwise falls back to a simple AND group.
        private static FilterGroup GetEffectiveFilterGroup(
            FilterGroup? filterGroup,
            int criterionCount
        )
        {
            if (filterGroup is null || filterGroup.Children.Count == 0)
            {
                return CreateAndGroup(criterionCount);
            }

            return filterGroup;
        }

        // Creates the fallback AND group used when no normalized tree was attached to the backend query.
        private static FilterGroup CreateAndGroup(int criterionCount)
        {
            var children = new List<FilterGroupNode>(criterionCount);
            for (var index = 0; index < criterionCount; index++)
            {
                children.Add(
                    new FilterGroupNode
                    {
                        CriterionIndex = index
                    }
                );
            }

            return new FilterGroup
            {
                Logic = FilterGroupLogic.And,
                Children = children
            };
        }

        // Recursively converts a filter group into one boolean expression tree.
        private static Expression<Func<TModel, bool>> BuildGroupExpression<TModel>(
            FilterGroup group,
            IReadOnlyList<Expression<Func<TModel, bool>>> criterionExpressions
        ) where TModel : class
        {
            if (group.Children.Count == 0)
            {
                throw new UnprocessableEntityException("Filter group must contain at least one child.");
            }

            var logic = group.Logic == default ? FilterGroupLogic.And : group.Logic;
            var children = group.Children
                .Select(child => BuildGroupNodeExpression<TModel>(child, criterionExpressions))
                .ToList();

            return logic switch
            {
                FilterGroupLogic.And => CombineExpressions(children, Expression.AndAlso),
                FilterGroupLogic.Or => CombineExpressions(children, Expression.OrElse),
                FilterGroupLogic.Not => BuildNotExpression(children),
                _ => throw new UnprocessableEntityException(
                    "Unsupported filter group logic.",
                    new Dictionary<string, object?>
                    {
                        ["logic"] = group.Logic
                    }
                )
            };
        }

        // Resolves one group node into either a nested group expression or one criterion expression.
        private static Expression<Func<TModel, bool>> BuildGroupNodeExpression<TModel>(
            FilterGroupNode node,
            IReadOnlyList<Expression<Func<TModel, bool>>> criterionExpressions
        ) where TModel : class
        {
            var hasCriterionIndex = node.CriterionIndex.HasValue;
            var hasGroup = node.Group is not null;

            if (hasCriterionIndex == hasGroup)
            {
                throw new UnprocessableEntityException("Each filter group node must define either a criterion index or a nested group.");
            }

            if (node.Group is not null)
            {
                return BuildGroupExpression(node.Group, criterionExpressions);
            }

            var criterionIndex = node.CriterionIndex!.Value;
            if (criterionIndex < 0 || criterionIndex >= criterionExpressions.Count)
            {
                throw new UnprocessableEntityException(
                    "Filter group references is invalid with the criterion index.",
                    new Dictionary<string, object?>
                    {
                        ["criterionIndex"] = criterionIndex
                    }
                );
            }

            return criterionExpressions[criterionIndex];
        }

        // Applies logical NOT to exactly one child expression.
        private static Expression<Func<TModel, bool>> BuildNotExpression<TModel>(
            IReadOnlyList<Expression<Func<TModel, bool>>> children
        ) where TModel : class
        {
            if (children.Count != 1)
            {
                throw new UnprocessableEntityException("NOT filter groups must contain exactly one child.");
            }

            var parameter = Expression.Parameter(typeof(TModel), "entity");
            var body = ReplaceParameter(children[0].Parameters[0], parameter, children[0].Body);

            return Expression.Lambda<Func<TModel, bool>>(Expression.Not(body), parameter);
        }

        // Converts one filter criterion into a predicate against the mapped entity field.
        private static Expression<Func<TModel, bool>> BuildCriterionExpression<TModel>(
            FilterCriterion criterion,
            FilterQueryMap<TModel> filterQueryMap
        ) where TModel : class
        {
            if (!filterQueryMap.TryGetField(criterion.FieldName, out var mappedField))
            {
                throw new UnprocessableEntityException(
                    "Unknown filter field.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName
                    }
                );
            }

            var values = (criterion.Values ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList();

            ValidateValueCount(criterion, values);

            var parameter = Expression.Parameter(typeof(TModel), "entity");
            var selectorBody = ReplaceParameter(
                mappedField.Selector.Parameters[0],
                parameter,
                mappedField.Selector.Body
            );

            if (RequiresNoValue(criterion.Operator))
            {
                var noValueBody = BuildNoValueExpression(criterion, selectorBody);
                return Expression.Lambda<Func<TModel, bool>>(noValueBody, parameter);
            }

            var valueExpressions = values
                .Select(value => BuildSingleValueExpression(
                    criterion,
                    selectorBody,
                    mappedField.FieldType,
                    value
                ))
                .ToList();

            var combinedBody = RequiresOrValueCombination(criterion.Operator)
                ? valueExpressions.Aggregate(Expression.OrElse)
                : valueExpressions.Aggregate(Expression.AndAlso);

            return Expression.Lambda<Func<TModel, bool>>(combinedBody, parameter);
        }

        // Handles operators that do not expect any values from the payload.
        private static Expression BuildNoValueExpression(
            FilterCriterion criterion,
            Expression memberExpression
        )
        {
            return criterion.Operator switch
            {
                FilterOperator.IsNull => BuildIsNullExpression(criterion, memberExpression),
                _ => throw new UnprocessableEntityException(
                    "Unsupported filter operator.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName,
                        ["operator"] = EnumUtils.ToString(criterion.Operator)
                    }
                )
            };
        }

        // Generates a null comparison and rejects fields that can never be null.
        private static Expression BuildIsNullExpression(
            FilterCriterion criterion,
            Expression memberExpression
        )
        {
            if (memberExpression.Type.IsValueType && Nullable.GetUnderlyingType(memberExpression.Type) is null)
            {
                throw new UnprocessableEntityException(
                    "This operator is only supported for nullable fields.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName,
                        ["operator"] = EnumUtils.ToString(criterion.Operator)
                    }
                );
            }

            return Expression.Equal(memberExpression, Expression.Constant(null, memberExpression.Type));
        }

        // Builds one comparison for one value after parsing and optional temporal transformation.
        private static Expression BuildSingleValueExpression(
            FilterCriterion criterion,
            Expression memberExpression,
            Type fieldType,
            string rawValue
        )
        {
            var nullableFieldType = Nullable.GetUnderlyingType(fieldType);
            var targetFieldType = nullableFieldType ?? fieldType;

            if (targetFieldType == typeof(bool) && criterion.Operator != FilterOperator.Equal)
            {
                throw new UnprocessableEntityException(
                    "Boolean fields only support the Equal operator.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName,
                        ["operator"] = EnumUtils.ToString(criterion.Operator)
                    }
                );
            }

            var nullGuard = GetNullGuard(memberExpression);
            var comparableExpression = UnwrapNullable(memberExpression);
            var comparisonExpression = comparableExpression;
            object parsedValue;

            if (criterion.DateTimeFilterOptions is not null)
            {
                (comparisonExpression, parsedValue) = ApplyDateTimeFilterOptions(
                    comparableExpression,
                    targetFieldType,
                    rawValue,
                    criterion.DateTimeFilterOptions
                );
            }
            else
            {
                parsedValue = ParseFilterValue(rawValue, targetFieldType, criterion.FieldName);
            }

            var body = criterion.Operator switch
            {
                FilterOperator.Equal => Expression.Equal(
                    comparisonExpression,
                    Expression.Constant(parsedValue, comparisonExpression.Type)
                ),
                FilterOperator.GreaterThan => Expression.GreaterThan(
                    comparisonExpression,
                    Expression.Constant(parsedValue, comparisonExpression.Type)
                ),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(
                    comparisonExpression,
                    Expression.Constant(parsedValue, comparisonExpression.Type)
                ),
                FilterOperator.LessThan => Expression.LessThan(
                    comparisonExpression,
                    Expression.Constant(parsedValue, comparisonExpression.Type)
                ),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(
                    comparisonExpression,
                    Expression.Constant(parsedValue, comparisonExpression.Type)
                ),
                FilterOperator.Contains => BuildStringOperation(
                    comparisonExpression,
                    nameof(string.Contains),
                    parsedValue
                ),
                FilterOperator.StartsWith => BuildStringOperation(
                    comparisonExpression,
                    nameof(string.StartsWith),
                    parsedValue
                ),
                FilterOperator.EndsWith => BuildStringOperation(
                    comparisonExpression,
                    nameof(string.EndsWith),
                    parsedValue
                ),
                _ => throw new UnprocessableEntityException(
                    "Unsupported filter operator.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName,
                        ["operator"] = EnumUtils.ToString(criterion.Operator)
                    }
                )
            };

            return nullGuard is null ? body : Expression.AndAlso(nullGuard, body);
        }

        // Applies date/month/year extraction when the request asks for temporal filtering.
        private static (Expression ComparisonExpression, object ParsedValue) ApplyDateTimeFilterOptions(
            Expression comparableExpression,
            Type fieldType,
            string rawValue,
            DateTimeFilterOptions options
        )
        {
            if (options.TemporalPartType == TemporalPartType.None)
            {
                return (
                    comparableExpression,
                    ParseFilterValue(rawValue, fieldType, null)
                );
            }

            if (fieldType != typeof(DateTimeOffset)
                && fieldType != typeof(DateTime)
                && fieldType != typeof(DateOnly))
            {
                throw new UnprocessableEntityException(
                    "Temporal filters are not supported for this type.",
                    new Dictionary<string, object?>
                    {
                        ["fieldType"] = fieldType.Name,
                        ["temporalPart"] = options.TemporalPartType
                    });
            }

            return options.TemporalPartType switch
            {
                TemporalPartType.Date => BuildDatePartComparison(
                    comparableExpression,
                    fieldType,
                    rawValue
                ),
                TemporalPartType.Month => (
                    Expression.Property(comparableExpression, nameof(DateTime.Month)),
                    int.Parse(rawValue, CultureInfo.InvariantCulture)
                ),
                TemporalPartType.Year => (
                    Expression.Property(comparableExpression, nameof(DateTime.Year)),
                    int.Parse(rawValue, CultureInfo.InvariantCulture)
                ),
                _ => throw new UnprocessableEntityException(
                    "Unsupported temporal part type.",
                    new Dictionary<string, object?>
                    {
                        ["temporalPart"] = options.TemporalPartType
                    })
            };
        }

        // Normalizes a date-only comparison for DateOnly and DateTime-like members.
        private static (Expression ComparisonExpression, object ParsedValue) BuildDatePartComparison(
            Expression comparableExpression,
            Type fieldType,
            string rawValue
        )
        {
            if (fieldType == typeof(DateOnly))
            {
                return (
                    comparableExpression,
                    DateOnly.Parse(rawValue, CultureInfo.InvariantCulture)
                );
            }

            return (
                Expression.Property(comparableExpression, nameof(DateTime.Date)),
                DateTime.Parse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Date
            );
        }

        // Translates string operators like contains and starts with into string method calls.
        private static Expression BuildStringOperation(
            Expression comparisonExpression,
            string methodName,
            object parsedValue
        )
        {
            if (comparisonExpression.Type != typeof(string))
            {
                throw new UnprocessableEntityException(
                    "This operator is only supported for string fields.",
                    new Dictionary<string, object?>
                    {
                        ["fieldType"] = comparisonExpression.Type.Name,
                        ["operator"] = methodName
                    });
            }

            var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
            return Expression.Call(
                comparisonExpression,
                method,
                Expression.Constant((string)parsedValue, typeof(string))
            );
        }

        // Parses the raw string payload into the concrete CLR type expected by the mapped field.
        private static object ParseFilterValue(string rawValue, Type targetType, string? fieldName)
        {
            try
            {
                if (targetType == typeof(string))
                {
                    return rawValue;
                }

                if (targetType == typeof(Guid))
                {
                    return Guid.Parse(rawValue);
                }

                if (targetType == typeof(int))
                {
                    return int.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType == typeof(long))
                {
                    return long.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType == typeof(decimal))
                {
                    return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType == typeof(float))
                {
                    return float.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType == typeof(double))
                {
                    return double.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType == typeof(bool))
                {
                    return bool.Parse(rawValue);
                }

                if (targetType == typeof(DateTimeOffset))
                {
                    return DateTimeOffset.Parse(
                        rawValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind
                    );
                }

                if (targetType == typeof(DateTime))
                {
                    return DateTime.Parse(
                        rawValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind
                    );
                }

                if (targetType == typeof(DateOnly))
                {
                    return DateOnly.Parse(rawValue, CultureInfo.InvariantCulture);
                }

                if (targetType.IsEnum)
                {
                    return ParseEnumDisplayValue(targetType, rawValue);
                }
            }
            catch (Exception exception) when (exception is not UnprocessableEntityException)
            {
                throw new UnprocessableEntityException(
                    "Invalid filter value for field.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = fieldName,
                        ["value"] = rawValue
                    }
                );
            }

            throw new UnprocessableEntityException(
                "Unsupported filter value type for field.",
                new Dictionary<string, object?>
                {
                    ["fieldName"] = fieldName,
                    ["type"] = targetType.Name
                }
            );
        }

        // Reuses the enum display-value converter so FE-facing enum values can be filtered directly.
        private static object ParseEnumDisplayValue(Type enumType, string rawValue)
        {
            var method = typeof(EnumUtils)
                .GetMethod(nameof(EnumUtils.ToString))!
                .MakeGenericMethod(enumType);

            try
            {
                return method.Invoke(null, [rawValue])!;
            }
            catch
            {
                throw new UnprocessableEntityException(
                    "Invalid filter value for enum.",
                    new Dictionary<string, object?>
                    {
                        ["type"] = enumType.Name,
                        ["value"] = rawValue
                    }
                );
            }
        }

        // Validates whether the operator received the correct number of values before building expressions.
        private static void ValidateValueCount(FilterCriterion criterion, IReadOnlyList<string> values)
        {
            if (RequiresNoValue(criterion.Operator))
            {
                if (values.Count != 0)
                {
                    throw new UnprocessableEntityException(
                        "Filter operator does not accept values.",
                        new Dictionary<string, object?>
                        {
                            ["fieldName"] = criterion.FieldName,
                            ["operator"] = EnumUtils.ToString(criterion.Operator),
                            ["valueCount"] = values.Count
                        }
                    );
                }

                return;
            }

            if (values.Count == 0)
            {
                throw new UnprocessableEntityException(
                    "Filter field must contain at least one value.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName
                    }
                );
            }

            if (!RequiresSingleValue(criterion.Operator))
            {
                return;
            }

            if (values.Count != 1)
            {
                throw new UnprocessableEntityException(
                    "Filter operator requires exactly one value.",
                    new Dictionary<string, object?>
                    {
                        ["fieldName"] = criterion.FieldName,
                        ["operator"] = EnumUtils.ToString(criterion.Operator),
                        ["valueCount"] = values.Count
                    }
                );
            }
        }

        // Comparison operators like > and <= accept exactly one value.
        private static bool RequiresSingleValue(FilterOperator filterOperator)
        {
            return filterOperator is FilterOperator.GreaterThan
                or FilterOperator.GreaterThanOrEqual
                or FilterOperator.LessThan
                or FilterOperator.LessThanOrEqual;
        }

        // Null-check operators do not accept any payload values.
        private static bool RequiresNoValue(FilterOperator filterOperator)
        {
            return filterOperator is FilterOperator.IsNull;
        }

        // These operators combine multiple input values with OR inside one criterion.
        private static bool RequiresOrValueCombination(FilterOperator filterOperator)
        {
            return filterOperator is FilterOperator.Equal
                or FilterOperator.Contains
                or FilterOperator.StartsWith
                or FilterOperator.EndsWith;
        }

        // Rebinds parameters so separately-built lambdas can be merged into one expression tree.
        private static Expression ReplaceParameter(
            ParameterExpression source,
            Expression target,
            Expression expression
        )
        {
            return new ReplaceParameterVisitor(source, target).Visit(expression)!;
        }

        // Combines many predicates into one predicate using the provided boolean operator.
        private static Expression<Func<TModel, bool>> CombineExpressions<TModel>(
            IReadOnlyList<Expression<Func<TModel, bool>>> expressions,
            Func<Expression, Expression, BinaryExpression> combiner
        ) where TModel : class
        {
            if (expressions.Count == 0)
            {
                return _ => true;
            }

            var parameter = Expression.Parameter(typeof(TModel), "entity");
            Expression? body = null;

            foreach (var expression in expressions)
            {
                var replacedBody = ReplaceParameter(expression.Parameters[0], parameter, expression.Body);
                body = body is null ? replacedBody : combiner(body, replacedBody);
            }

            return Expression.Lambda<Func<TModel, bool>>(body!, parameter);
        }

        // Adds a null guard before member access when the field is nullable.
        private static Expression? GetNullGuard(Expression memberExpression)
        {
            if (!memberExpression.Type.IsValueType
                || Nullable.GetUnderlyingType(memberExpression.Type) is not null)
            {
                return Expression.NotEqual(memberExpression, Expression.Constant(null, memberExpression.Type));
            }

            return null;
        }

        // Converts Nullable<T> member access into its Value expression for typed comparisons.
        private static Expression UnwrapNullable(Expression expression)
        {
            return Nullable.GetUnderlyingType(expression.Type) is null
                ? expression
                : Expression.Property(expression, nameof(Nullable<int>.Value));
        }

        // Expression visitor used to replace one lambda parameter with another expression.
        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression source;
            private readonly Expression target;

            public ReplaceParameterVisitor(ParameterExpression source, Expression target)
            {
                this.source = source;
                this.target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == source ? target : base.VisitParameter(node);
            }
        }
    }

    private static class SortingExpression
    {
        // Applies all requested sorts in order using the field selectors from FilterQueryMap.
        public static IQueryable<TModel> ApplySorting<TModel>(
            IQueryable<TModel> query,
            FilterQuery<TModel> filterQuery
        ) where TModel : class
        {
            var sortFields = filterQuery.SortFields;
            if (sortFields is null || sortFields.Count == 0)
            {
                return query;
            }

            var filterQueryMap = filterQuery.GetFilterQueryMap();
            IOrderedQueryable<TModel>? orderedQuery = null;

            foreach (var sortField in sortFields)
            {
                if (!filterQueryMap.TryGetField(sortField.FieldName, out var mappedField))
                {
                    throw new UnprocessableEntityException(
                        "Unknown sort field.",
                        new Dictionary<string, object?>
                        {
                            ["fieldName"] = sortField.FieldName
                        }
                    );
                }

                orderedQuery = ApplyOrdering(
                    orderedQuery ?? query,
                    mappedField.Selector,
                    sortField.IsAscending,
                    orderedQuery is null
                );
            }

            return orderedQuery ?? query;
        }

        // Chooses OrderBy/ThenBy and ascending/descending variants for one sort field.
        private static IOrderedQueryable<TModel> ApplyOrdering<TModel>(
            IQueryable<TModel> query,
            LambdaExpression selector,
            bool isAscending,
            bool isFirstOrdering
        ) where TModel : class
        {
            var methodName = isFirstOrdering
                ? (isAscending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending))
                : (isAscending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending));

            var orderingMethod = typeof(Queryable)
                .GetMethods()
                .Single(method =>
                    method.Name == methodName
                    && method.GetParameters().Length == 2
                )
                .MakeGenericMethod(typeof(TModel), selector.ReturnType);

            return (IOrderedQueryable<TModel>)orderingMethod.Invoke(null, [query, selector])!;
        }
    }

    private static class PaginationExpression
    {
        public static async Task<FilterResult<TResult>> ToFilterResultAsync<TModel, TResult>(
            IQueryable<TModel> query,
            FilterQuery<TModel> filterQuery,
            Expression<Func<TModel, TResult>> selector,
            CancellationToken cancellationToken
        )
            where TModel : class
            where TResult : class
        {
            var totalItems = await query.CountAsync(cancellationToken);

            var items = await query
                .ApplyPaging(filterQuery)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new FilterResult<TResult>
            {
                Items = items,
                CurrentPage = filterQuery.CurrentPage,
                ItemsPerPage = filterQuery.ItemsPerPage,
                TotalItems = totalItems,
                TotalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)filterQuery.ItemsPerPage)
            };
        }
    }
}
