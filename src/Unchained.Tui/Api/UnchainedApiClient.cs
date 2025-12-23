using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Unchained.Tui.Common;

namespace Unchained.Tui.Api;

public class UnchainedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnchainedApiClient> _logger;
    private readonly AppState _state;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public UnchainedApiClient(HttpClient httpClient, ILogger<UnchainedApiClient> logger, AppState state)
    {
        _httpClient = httpClient;
        _logger = logger;
        _state = state;
        ApplyHttpClientOptions(state.Options);
        _state.Changed += ApplyHttpClientOptions;
    }

    public Task<ApiResult<IReadOnlyList<ChannelDto>>> GetChannelsAsync(CancellationToken cancellationToken)
        => SendAsync<IReadOnlyList<ChannelDto>>(HttpMethod.Get, "/channels", null, cancellationToken, allowRetry: true);

    public Task<ApiResult<IReadOnlyList<EpgEventDto>>> GetEpgAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        => SendAsync<IReadOnlyList<EpgEventDto>>(HttpMethod.Get, $"/epg?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}", null, cancellationToken, allowRetry: true);

    public Task<ApiResult<string>> DownloadM3uAsync(string profile, CancellationToken cancellationToken)
        => SendRawAsync(HttpMethod.Get, $"/m3u?profile={Uri.EscapeDataString(profile)}", cancellationToken, allowRetry: true);

    public Task<ApiResult<string>> DownloadXmlTvAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        => SendRawAsync(HttpMethod.Get, $"/xmltv?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}", cancellationToken, allowRetry: true);

    public Task<ApiResult<string>> GetHealthAsync(bool live, CancellationToken cancellationToken)
        => SendRawAsync(HttpMethod.Get, live ? "/health/live" : "/health/ready", cancellationToken, allowRetry: true);

    public Task<ApiResult<StatusDto>> GetStatusAsync(CancellationToken cancellationToken)
        => SendAsync<StatusDto>(HttpMethod.Get, "/status", null, cancellationToken, allowRetry: true);

    public Task<ApiResult<string>> PostAdminAsync(string path, CancellationToken cancellationToken)
        => SendRawAsync(HttpMethod.Post, path, cancellationToken, allowRetry: false);

    private Uri BuildUri(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        var normalized = AppState.NormalizeBaseUrl(_state.Options.BaseUrl);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("BaseUrl missing");
        }

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private void ApplyHttpClientOptions(UnchainedOptions options)
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.Http.TimeoutSeconds));
        var normalized = AppState.NormalizeBaseUrl(options.BaseUrl);
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var baseUri))
        {
            _httpClient.BaseAddress = baseUri;
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (_state.Options.Auth.Mode == AuthMode.ApiKey && !string.IsNullOrWhiteSpace(_state.Options.Auth.ApiKey))
        {
            var header = string.IsNullOrWhiteSpace(_state.Options.Auth.ApiKeyHeader) ? "X-Api-Key" : _state.Options.Auth.ApiKeyHeader;
            request.Headers.Remove(header);
            request.Headers.Add(header, _state.Options.Auth.ApiKey);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? content, CancellationToken cancellationToken, bool allowRetry)
    {
        var attempts = allowRetry && method == HttpMethod.Get ? 2 : 1;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, BuildUri(path));
                ApplyAuth(request);
                if (content != null)
                {
                    request.Content = JsonContent.Create(content);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken).ConfigureAwait(false);
                    if (data == null)
                    {
                        return ApiResult<T>.FromMessage("Empty response", response.StatusCode);
                    }
                    return ApiResult<T>.FromData(data, response.StatusCode);
                }

                return ApiResult<T>.FromError(await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false), response.StatusCode);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && i + 1 < attempts)
            {
                _logger.LogWarning("Request timeout, retrying {Path}", path);
            }
            catch (HttpRequestException ex) when (i + 1 < attempts)
            {
                _logger.LogWarning(ex, "Transient error calling {Path}", path);
            }
        }

        return ApiResult<T>.FromMessage("Request failed", null);
    }

    private async Task<ApiResult<string>> SendRawAsync(HttpMethod method, string path, CancellationToken cancellationToken, bool allowRetry)
    {
        var attempts = allowRetry && method == HttpMethod.Get ? 2 : 1;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, BuildUri(path));
                ApplyAuth(request);
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return ApiResult<string>.FromData(content, response.StatusCode);
                }

                return ApiResult<string>.FromError(await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false), response.StatusCode);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && i + 1 < attempts)
            {
                _logger.LogWarning("Request timeout, retrying {Path}", path);
            }
            catch (HttpRequestException ex) when (i + 1 < attempts)
            {
                _logger.LogWarning(ex, "Transient error calling {Path}", path);
            }
        }

        return ApiResult<string>.FromMessage("Request failed", null);
    }

    private async Task<ApiError> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetailsEnvelope>(raw, SerializerOptions);
            if (problem != null)
            {
                return new ApiError
                {
                    Title = problem.Title,
                    Detail = problem.Detail,
                    Status = (HttpStatusCode?)problem.Status ?? response.StatusCode,
                    TraceId = problem.TraceId,
                    CorrelationId = problem.CorrelationId,
                    Type = problem.Type,
                    Extensions = problem.Extensions,
                    Raw = raw
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse error response");
        }

        return new ApiError
        {
            Detail = string.IsNullOrWhiteSpace(raw) ? response.ReasonPhrase : raw,
            Status = response.StatusCode,
            Raw = raw
        };
    }

    private class ProblemDetailsEnvelope
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public int? Status { get; set; }
        public string? Type { get; set; }
        public string? TraceId { get; set; }
        public string? CorrelationId { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }
}
