using System.Text.Json;
using HandlebarsDotNet;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Infrastructure.Services.Email;

public static class HandlebarsEmailBinder
{
    public static string BindTemplate(string template, JsonDocument? data)
    {
        try
        {
            var compiledTemplate = Handlebars.Compile(template);
            return compiledTemplate(ToHandlebarsModel(data?.RootElement));
        }
        catch (HandlebarsException exception)
        {
            throw new InvalidEmailTemplateException(
                "The email template is not a valid Handlebars template.",
                innerException: exception);
        }
    }

    private static IReadOnlyDictionary<string, object?> ToHandlebarsModel(JsonElement? root)
    {
        if (root is not { ValueKind: JsonValueKind.Object } objectRoot)
        {
            return new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);
        }

        return ToHandlebarsObject(objectRoot);
    }

    private static Dictionary<string, object?> ToHandlebarsObject(JsonElement objectElement)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in objectElement.EnumerateObject())
        {
            result[property.Name] = ToHandlebarsValue(property.Value);
        }

        return result;
    }

    private static object? ToHandlebarsValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object =>
                ToHandlebarsObject(value),

            JsonValueKind.Array =>
                value.EnumerateArray()
                    .Select(ToHandlebarsValue)
                    .ToList(),

            JsonValueKind.String =>
                value.GetString(),

            JsonValueKind.Number =>
                value.TryGetInt64(out var integer)
                    ? integer
                    : value.TryGetDecimal(
                        out var decimalValue)
                        ? decimalValue
                        : value.GetDouble(),

            JsonValueKind.True => true,
            JsonValueKind.False => false,

            JsonValueKind.Null
                or JsonValueKind.Undefined => null,

            _ => value.GetRawText()
        };
    }
}