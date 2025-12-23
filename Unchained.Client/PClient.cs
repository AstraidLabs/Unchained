using System.Net;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Unchained.Client.Models;
using Unchained.Client.Models.Session;

namespace Unchained.Client;

/// <summary>
///     High level HTTP client used to communicate with a running instance of
///     the Unchained API. The client wraps all available endpoints and returns
///     deserialized <see cref="ApiResponse{T}"/> objects.
/// </summary>
public class PClient : IPClient, IDisposable, IAsyncDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     Container with cookies gathered from HTTP responses. It is shared
    ///     with the underlying <see cref="HttpClient"/> so that authentication
    ///     cookies are automatically sent with subsequent requests.
    /// </summary>
    public CookieContainer Cookies { get; } = new();

    /// <summary>
    ///     Creates a new <see cref="PClient"/> targeting the specified base
    ///     URL. A dedicated <see cref="HttpClient"/> instance is created and
    ///     configured to send and receive JSON by default.
    /// </summary>
    /// <param name="baseUrl">Base address of the Unchained API.</param>
    public PClient(string baseUrl)
    {
        var handler = new HttpClientHandler { CookieContainer = Cookies };
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    ///     Internal constructor used mainly for unit tests where an existing
    ///     <see cref="HttpClient"/> is supplied.
    /// </summary>
    /// <param name="httpClient">Pre-configured HTTP client.</param>
    internal PClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    ///     Sends the prepared HTTP request and converts the response into
    ///     <see cref="ApiResponse{T}"/>. Some endpoints may return plain text
    ///     rather than JSON; in such cases the text is wrapped into the
    ///     <see cref="ApiResponse{T}.Data"/> property directly.
    /// </summary>
    /// <typeparam name="T">Type to which the "data" part of the response should be deserialized.</typeparam>
    /// <param name="request">Request message to send.</param>
    /// <returns>Structured API response with deserialized data.</returns>
    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await _httpClient.SendAsync(request);
        var contentType = response.Content.Headers.ContentType?.MediaType;

        // Endpoints like playlist and EPG XML return raw text instead of JSON
        if (contentType is not null && !contentType.Contains("json") && typeof(T) == typeof(string))
        {
            var text = await response.Content.ReadAsStringAsync();

            return new ApiResponse<T>
            {
                Success = response.IsSuccessStatusCode,
                Data = response.IsSuccessStatusCode ? (T)(object)text : default,
                Message = response.ReasonPhrase,
                Errors = response.IsSuccessStatusCode
                    ? new List<string>()
                    : new List<string> { response.ReasonPhrase ?? "Error" },
                Timestamp = DateTime.UtcNow
            };
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>()
            ?? new ApiResponse<T> { Timestamp = DateTime.UtcNow };

        if (!response.IsSuccessStatusCode)
        {
            apiResponse.Success = false;
            if (apiResponse.Errors.Count == 0)
            {
                apiResponse.Errors.Add(response.ReasonPhrase ?? "Error");
            }
        }

        return apiResponse;
    }

    /// <summary>
    ///     Performs user login and stores received authentication cookies in
    ///     <see cref="Cookies"/>. The returned object contains basic session
    ///     information such as the identifier of the created session.
    /// </summary>
    /// <param name="dto">Login credentials and additional metadata.</param>
    /// <returns>Result of the login attempt including session data.</returns>
    public async Task<ApiResponse<SessionCreatedDto>> LoginAsync(LoginDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/login")
        {
            Content = JsonContent.Create(dto)
        };

        return await SendAsync<SessionCreatedDto>(request);
    }

    /// <summary>
    ///     Terminates the current session on the server and removes the
    ///     associated authentication cookies.
    /// </summary>
    /// <returns>Operation result indicating whether the logout succeeded.</returns>
    public async Task<ApiResponse<string>> LogoutAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Retrieves the authentication status for the current session. This
    ///     can be used to verify whether the stored cookies are still valid
    ///     or if re-authentication is required.
    /// </summary>
    /// <returns>Current authentication status.</returns>
    public async Task<ApiResponse<AuthStatusDto>> GetAuthStatusAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "magenta/auth/status");
        return await SendAsync<AuthStatusDto>(request);
    }

    /// <summary>
    ///     Returns the list of available TV channels. Data can optionally be
    ///     refreshed on the server before it is returned.
    /// </summary>
    /// <param name="refresh">If set to <c>true</c>, forces the server to refresh cached channel information.</param>
    /// <returns>Collection of channel descriptors.</returns>
    public async Task<ApiResponse<List<ChannelDto>>> GetChannelsAsync(bool refresh = false)
    {
        var url = "magenta/channels";
        if (refresh)
        {
            url += "?refresh=true";
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<List<ChannelDto>>(request);
    }

    /// <summary>
    ///     Retrieves information about multiple channels in a single request.
    /// </summary>
    /// <param name="channelIds">Identifiers of channels to query.</param>
    /// <returns>List of channel details for the provided identifiers.</returns>
    public async Task<ApiResponse<List<ChannelDto>>> GetChannelsBulkAsync(IEnumerable<int> channelIds)
    {
        var idList = string.Join(',', channelIds);
        var request = new HttpRequestMessage(HttpMethod.Get, $"magenta/channels/bulk?ids={idList}");
        return await SendAsync<List<ChannelDto>>(request);
    }

    /// <summary>
    ///     Retrieves electronic program guide (EPG) data for a single channel.
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <param name="from">Optional start time of the interval.</param>
    /// <param name="to">Optional end time of the interval.</param>
    /// <returns>List of program items.</returns>
    public async Task<ApiResponse<List<EpgItemDto>>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool refresh = false)
    {
        var url = $"magenta/epg/{channelId}";
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={from:O}");
        if (to.HasValue) query.Add($"to={to:O}");
        if (refresh) query.Add("refresh=true");
        if (query.Count > 0) url += "?" + string.Join("&", query);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<List<EpgItemDto>>(request);
    }

    /// <summary>
    ///     Retrieves EPG data for multiple channels at once.
    /// </summary>
    /// <param name="channelIds">IDs of channels to query.</param>
    /// <param name="from">Optional start time of the interval.</param>
    /// <param name="to">Optional end time of the interval.</param>
    /// <returns>Dictionary mapping channel IDs to EPG items.</returns>
    public async Task<ApiResponse<Dictionary<int, List<EpgItemDto>>>> GetEpgBulkAsync(IEnumerable<int> channelIds, DateTimeOffset? from = null, DateTimeOffset? to = null, bool refresh = false)
    {
        var idList = string.Join(",", channelIds);
        var query = new List<string> { $"ids={idList}" };
        if (from.HasValue) query.Add($"from={from:O}");
        if (to.HasValue) query.Add($"to={to:O}");
        if (refresh) query.Add("refresh=true");
        var url = "magenta/epg/bulk?" + string.Join("&", query);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<Dictionary<int, List<EpgItemDto>>>(request);
    }

    /// <summary>
    ///     Returns a direct streaming URL for the specified channel.
    /// </summary>
    /// <param name="channelId">Identifier of the channel to stream.</param>
    /// <returns>URL information used to start playback.</returns>
    public async Task<ApiResponse<StreamUrlDto>> GetStreamUrlAsync(int channelId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"magenta/stream/{channelId}");
        return await SendAsync<StreamUrlDto>(request);
    }

    /// <summary>
    ///     Retrieves streaming URLs for multiple channels in bulk.
    /// </summary>
    /// <param name="channelIds">Collection of channel IDs.</param>
    /// <returns>Dictionary where keys are channel IDs and values are stream URLs.</returns>
    public async Task<ApiResponse<Dictionary<int, string?>>> GetStreamUrlsBulkAsync(IEnumerable<int> channelIds)
    {
        var idList = string.Join(',', channelIds);
        var request = new HttpRequestMessage(HttpMethod.Get, $"magenta/stream/bulk?ids={idList}");
        return await SendAsync<Dictionary<int, string?>>(request);
    }

    /// <summary>
    ///     Obtains a catch-up stream URL for a previously aired program.
    /// </summary>
    /// <param name="scheduleId">Identifier of the schedule item.</param>
    /// <returns>Stream URL for the specified recording.</returns>
    public async Task<ApiResponse<StreamUrlDto>> GetCatchupStreamAsync(long scheduleId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"magenta/catchup/{scheduleId}");
        return await SendAsync<StreamUrlDto>(request);
    }

    /// <summary>
    ///     Starts a DVR recording on the server for the given channel.
    /// </summary>
    /// <param name="channelId">Channel to record.</param>
    /// <param name="minutes">Duration of the recording in minutes.</param>
    /// <returns>Identifier of the created FFmpeg job.</returns>
    public async Task<ApiResponse<string>> StartRecordingAsync(int channelId, int minutes = 60)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"dvr/start/{channelId}?minutes={minutes}");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Queries the status of an FFmpeg recording job.
    /// </summary>
    /// <param name="jobId">Identifier of the recording job.</param>
    /// <returns>Current status details.</returns>
    public async Task<ApiResponse<FfmpegJobStatus>> GetRecordingStatusAsync(string jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"dvr/status/{jobId}");
        return await SendAsync<FfmpegJobStatus>(request);
    }


    /// <summary>
    ///     Downloads an M3U playlist containing all available channels.
    /// </summary>
    /// <returns>Raw text of the playlist.</returns>
    public async Task<ApiResponse<string>> GetPlaylistAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "magenta/playlist");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Retrieves EPG information in XML format for the specified channel.
    ///     This is useful for applications that consume the XMLTV standard.
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <returns>Raw XML string.</returns>
    public async Task<ApiResponse<string>> GetEpgXmlAsync(int channelId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"magenta/epgxml/{channelId}");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Performs a simple ping to check whether the server is reachable and
    ///     responsive.
    /// </summary>
    /// <returns>Information about the ping time and server version.</returns>
    public async Task<ApiResponse<PingResultDto>> PingAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "magenta/ping");
        return await SendAsync<PingResultDto>(request);
    }

    /// <summary>
    ///     Retrieves a list of devices currently registered with the account.
    /// </summary>
    /// <returns>Collection of device descriptions.</returns>
    public async Task<ApiResponse<List<DeviceInfoDto>>> GetDevicesAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "magenta/devices");
        return await SendAsync<List<DeviceInfoDto>>(request);
    }

    /// <summary>
    ///     Deletes a registered device from the user's account.
    /// </summary>
    /// <param name="id">Identifier of the device to remove.</param>
    /// <returns>Result message from the server.</returns>
    public async Task<ApiResponse<string>> DeleteDeviceAsync(string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"magenta/devices/{id}");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Returns details about the session associated with the current cookies.
    /// </summary>
    /// <returns>Information about the active session.</returns>
    public async Task<ApiResponse<SessionInfoDto>> GetCurrentSessionAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "sessions/current");
        return await SendAsync<SessionInfoDto>(request);
    }

    /// <summary>
    ///     Retrieves all active sessions for the current user.
    /// </summary>
    /// <returns>List of sessions associated with the account.</returns>
    public async Task<ApiResponse<List<SessionDto>>> GetUserSessionsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "sessions/user");
        return await SendAsync<List<SessionDto>>(request);
    }

    /// <summary>
    ///     Explicitly revokes a specific session by its identifier.
    /// </summary>
    /// <param name="sessionId">ID of the session to revoke.</param>
    /// <returns>Result message from the server.</returns>
    public async Task<ApiResponse<string>> RevokeSessionAsync(string sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"sessions/revoke/{sessionId}");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Logs out all sessions except the one represented by the current cookies.
    /// </summary>
    /// <returns>Result message confirming the operation.</returns>
    public async Task<ApiResponse<string>> LogoutAllOtherSessionsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "sessions/logout-all");
        return await SendAsync<string>(request);
    }

    /// <summary>
    ///     Retrieves aggregated statistics about all active sessions.
    /// </summary>
    /// <returns>Session statistics including counts and activity data.</returns>
    public async Task<ApiResponse<SessionStatistics>> GetSessionStatisticsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "sessions/statistics");
        return await SendAsync<SessionStatistics>(request);
    }

    /// <summary>
    ///     Attempts to discover a running Unchained server on the local network
    ///     using a UDP broadcast message.
    /// </summary>
    /// <param name="port">UDP port used for discovery.</param>
    /// <param name="timeoutMs">Timeout in milliseconds for waiting on a response.</param>
    /// <returns>Base URL of the discovered server or <c>null</c> if none responded.</returns>
    public static async Task<string?> DiscoverServerAsync(int port = 15998, int timeoutMs = 3000)
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        var request = Encoding.UTF8.GetBytes("MAGENTATV_DISCOVERY_REQUEST");
        await udp.SendAsync(request, request.Length, new IPEndPoint(IPAddress.Broadcast, port));

        var receiveTask = udp.ReceiveAsync();
        var completed = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));
        if (completed == receiveTask)
        {
            var result = await receiveTask;
            var message = Encoding.UTF8.GetString(result.Buffer);
            const string prefix = "MAGENTATV_DISCOVERY_RESPONSE|";
            if (message.StartsWith(prefix))
            {
                return message.Substring(prefix.Length);
            }
        }
        return null;
    }

    /// <summary>
    ///     Releases resources used by the underlying <see cref="HttpClient"/>.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    ///     Asynchronously releases resources used by the <see cref="HttpClient"/>.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
