using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unchained.Tui.Common;

public static class Formatters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public static string Trimmed(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
