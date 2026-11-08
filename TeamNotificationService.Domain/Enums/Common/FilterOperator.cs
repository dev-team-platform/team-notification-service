namespace TeamNotificationService.Domain.Enums.Common;

public enum FilterOperator
{
    Equals = 1,
    GreaterThan = 2,
    LessThan = 3,
    GreaterThanOrEquals = 4,
    LessThanOrEquals = 5,
    Contains = 6,
    StartsWith = 7,
    EndsWith = 8,
    IsNull = 9,
}