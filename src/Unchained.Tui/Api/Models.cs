using System.Net;
using System.Text.Json.Serialization;

namespace Unchained.Tui.Api;

public record ChannelDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("groupTitle")]
    public string? GroupTitle { get; init; }

    [JsonPropertyName("tvgId")]
    public string? TvgId { get; init; }

    [JsonPropertyName("logo")]
    public string? Logo { get; init; }

    [JsonPropertyName("streamUrl")]
    public string? StreamUrl { get; init; }
}

public record EpgEventDto
{
    [JsonPropertyName("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset End { get; init; }
}

public record StatusDto
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("uptime")]
    public string? Uptime { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; init; }
}

public record ApiError
{
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public string? TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public HttpStatusCode? Status { get; init; }
    public string? Type { get; init; }
    public Dictionary<string, object?>? Extensions { get; init; }
    public string? Raw { get; init; }
}

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public string? Message { get; init; }

    public static ApiResult<T> FromData(T data, HttpStatusCode status) => new()
    {
        Success = true,
        Data = data,
        StatusCode = status
    };

    public static ApiResult<T> FromError(ApiError error, HttpStatusCode? status = null, string? message = null) => new()
    {
        Success = false,
        Error = error,
        StatusCode = status,
        Message = message ?? error.Detail ?? error.Title
    };

    public static ApiResult<T> FromMessage(string message, HttpStatusCode? status = null) => new()
    {
        Success = false,
        Message = message,
        StatusCode = status
    };
}
