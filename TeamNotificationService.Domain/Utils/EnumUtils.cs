using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Domain.Utils;

public static class EnumUtils
{
    public static T Parse<T>(string value) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new UnprocessableEntityException($"'{value}' is not a valid value for enum of type {typeof(T).Name}",
            new Dictionary<string, object>
            {
                ["EnumType"] = typeof(T).Name,
                ["Value"] = value
            });
    }

    public static string ToString<T>(T enumValue) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (!Enum.IsDefined(typeof(T), enumValue))
        {
            throw new UnprocessableEntityException($"'{enumValue}' is not a valid value for enum of type {typeof(T).Name}",
                new Dictionary<string, object>
                {
                    ["EnumType"] = typeof(T).Name,
                    ["EnumValue"] = enumValue
                });
        }

        return enumValue.ToString()!;
    }
}