using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Domain.Utils;

public static class EnumUtils
{
    public static T Parse<T>(string value) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            throw new UnprocessableEntityException("T must be an enumerated type");
        }

        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new UnprocessableEntityException(
            "This is not a valid value for the enum.",
            new Dictionary<string, object?>
            {
                ["enumType"] = typeof(T).Name,
                ["value"] = value
            });
    }

    public static string ToString<T>(T enumValue) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            throw new UnprocessableEntityException("T must be an enumerated type");
        }

        if (!Enum.IsDefined(typeof(T), enumValue))
        {
            throw new UnprocessableEntityException(
                "This is not a valid value for the enum.",
                new Dictionary<string, object?>
                {
                    ["enumType"] = typeof(T).Name,
                    ["enumValue"] = enumValue
                });
        }

        return enumValue.ToString()!;
    }
}
