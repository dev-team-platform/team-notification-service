using System.Text.Json;

namespace TeamNotificationService.Domain.Utils;

public static class JsonUtils
{
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web);
}
