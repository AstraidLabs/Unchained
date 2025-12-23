using Unchained.Configuration;
using Unchained.Models;
using Unchained.Services;
using Unchained.Services.TokenStorage;
using Unchained.Services.Device;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Unchained.Services
{
    /// <summary>
    /// High level client responsible for communicating with the Unchained API
    /// and caching results. It also manages authentication tokens via
    /// <see cref="ITokenStorage"/>.
    /// </summary>
    public class Unchained : IUnchained
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<Unchained> _logger;
        private readonly UnchainedOptions _options;
        private readonly CacheOptions _cacheOptions;
        private readonly TokenStorageOptions _tokenOptions;
        private readonly ITokenStorage _tokenStorage;
        private readonly NetworkOptions _networkOptions;
        private readonly IDeviceIdProvider _deviceIdProvider;
        private string? _devId;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        
        // Current session tokens
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public Unchained(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<Unchained> logger,
            IOptions<UnchainedOptions> options,
            IOptions<CacheOptions> cacheOptions,
            IOptions<TokenStorageOptions> tokenOptions,
            ITokenStorage tokenStorage,
            IOptions<NetworkOptions> networkOptions,
            IDeviceIdProvider deviceIdProvider)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
            _cacheOptions = cacheOptions.Value;
            _tokenOptions = tokenOptions.Value;
            _tokenStorage = tokenStorage;
            _networkOptions = networkOptions.Value;
            _deviceIdProvider = deviceIdProvider;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                _devId = await _deviceIdProvider.GetDeviceIdAsync();
                ConfigureHttpClient();

                if (_tokenOptions.AutoLoad)
                {
                    await LoadStoredTokensAsync();
                }

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task LoadStoredTokensAsync()
        {
            try
            {
                var tokens = await _tokenStorage.LoadTokensAsync();
                if (tokens?.IsValid == true)
                {
                    _accessToken = tokens.AccessToken;
                    _refreshToken = tokens.RefreshToken;
                    _tokenExpiry = tokens.ExpiresAt;

                    _logger.LogInformation("Loaded valid tokens for user: {Username}, expires: {ExpiresAt}",
                        tokens.Username, tokens.ExpiresAt);
                }
                else if (tokens != null)
                {
                    _logger.LogInformation("Loaded expired tokens for user: {Username}, expired: {ExpiresAt}",
                        tokens.Username, tokens.ExpiresAt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load stored tokens on startup");
            }
        }

        /// <summary>
        /// Performs a full login flow using the provided user credentials.
        /// The method initializes a device session, authenticates the user and
        /// persists the received tokens if configured to do so.
        /// </summary>
        /// <param name="username">Unchained account name.</param>
        /// <param name="password">Password for the account.</param>
        /// <returns><c>true</c> when login succeeded.</returns>
        public async Task<bool> LoginAsync(string username, string password)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await InitializeAsync();
                _logger.LogInformation("Attempting login for user: {Username}", username);

                // Step 1: Initialize session
                var accessToken = await InitializeSessionAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Failed to initialize session");
                    return false;
                }

                // Step 2: Login with credentials
                var loginSuccess = await PerformLoginAsync(accessToken, username, password);
                if (loginSuccess)
                {
                    _logger.LogInformation("Login successful for user: {Username} in {ElapsedMs}ms",
                        username, stopwatch.ElapsedMilliseconds);

                    // Save tokens to storage if auto-save is enabled
                    if (_tokenOptions.AutoSave && !string.IsNullOrEmpty(_accessToken))
                    {
                        var tokenData = new TokenData
                        {
                            AccessToken = _accessToken,
                            RefreshToken = _refreshToken ?? "",
                            ExpiresAt = _tokenExpiry,
                            Username = username,
                            DeviceId = _devId,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _tokenStorage.SaveTokensAsync(tokenData);
                        _logger.LogDebug("Tokens saved to storage for user: {Username}", username);
                    }
                }
                else
                {
                    _logger.LogWarning("Login failed for user: {Username} after {ElapsedMs}ms",
                        username, stopwatch.ElapsedMilliseconds);
                }

                return loginSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {Username} after {ElapsedMs}ms",
                    username, stopwatch.ElapsedMilliseconds);
                return false;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task LogoutAsync(string sessionId)
        {
            try
            {
                await InitializeAsync();
                _logger.LogInformation("Logging out...");

                // Clear tokens from memory
                _accessToken = null;
                _refreshToken = null;
                _tokenExpiry = DateTime.MinValue;

                // Clear tokens from storage
                await _tokenStorage.ClearTokensAsync(sessionId);

                _logger.LogInformation("Logout completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        private async Task<string?> InitializeSessionAsync()
        {
            try
            {
                await InitializeAsync();
                var initParams = new Dictionary<string, string>
                {
                    {"dsid", _devId},
                    {"deviceName", _options.DeviceName},
                    {"deviceType", _options.DeviceType},
                    {"osVersion", "0.0.0"},
                    {"appVersion", "4.0.27.0"},
                    {"language", _options.Language.ToUpper()},
                    {"devicePlatform", "GO"}
                };

                var initUri = $"{_options.BaseUrl}/{_options.ApiVersion}/auth/init?" +
                             string.Join("&", initParams.Select(x => $"{x.Key}={x.Value}"));

                var response = await _httpClient.PostAsync(initUri, null);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (json.RootElement.TryGetProperty("token", out var token) &&
                    token.TryGetProperty("accessToken", out var accessTokenProp))
                {
                    var accessToken = accessTokenProp.GetString();
                    _logger.LogDebug("Session initialized successfully");
                    return accessToken;
                }

                _logger.LogWarning("No access token in session initialization response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize session");
                throw;
            }
        }

        private async Task<bool> PerformLoginAsync(string sessionToken, string username, string password)
        {
            try
            {
                var loginBody = new { loginOrNickname = username, password = password };
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/{_options.ApiVersion}/auth/login")
                {
                    Content = new StringContent(JsonSerializer.Serialize(loginBody), Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (json.RootElement.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    _accessToken = json.RootElement.GetProperty("token").GetProperty("accessToken").GetString();
                    _refreshToken = json.RootElement.GetProperty("token").GetProperty("refreshToken").GetString();
                    _tokenExpiry = DateTime.UtcNow.AddHours(_tokenOptions.TokenExpirationHours);

                    _logger.LogDebug("Login performed successfully for user: {Username}", username);
                    return true;
                }

                _logger.LogWarning("Login unsuccessful - invalid credentials for user: {Username}", username);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform login for user: {Username}", username);
                throw;
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            // Check if current tokens are valid
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return;

            // Try to load from storage
            var tokens = await _tokenStorage.LoadTokensAsync();
            if (tokens?.IsValid == true)
            {
                _accessToken = tokens.AccessToken;
                _refreshToken = tokens.RefreshToken;
                _tokenExpiry = tokens.ExpiresAt;
                _logger.LogDebug("Loaded valid tokens from storage for user: {Username}", tokens.Username);
                return;
            }

            // If storage doesn't have valid tokens either
            throw new UnauthorizedAccessException("Authentication required. Please login first.");
        }

        public async Task<List<ChannelDto>> GetChannelsAsync(bool forceRefresh = false)
        {
            const string cacheKey = "channels";

            await InitializeAsync();

            if (!forceRefresh && _cache.TryGetValue(cacheKey, out List<ChannelDto>? cached))
            {
                _logger.LogDebug("Returning cached channels");
                return cached!;
            }

            if (forceRefresh)
            {
                _logger.LogDebug("Force refresh requested, bypassing channel cache");
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await EnsureAuthenticatedAsync();

                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{_options.BaseUrl}/{_options.ApiVersion}/television/channels?list=LIVE&queryScope=LIVE");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var channels = ParseChannels(json);

                var cacheExpiry = TimeSpan.FromMinutes(_cacheOptions.ChannelsExpirationMinutes);
                _cache.Set(cacheKey, channels, cacheExpiry);

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {ChannelCount} channels in {Duration}ms",
                    channels.Count, stopwatch.ElapsedMilliseconds);

                return channels;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to get channels after {Duration}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private List<ChannelDto> ParseChannels(JsonDocument json)
        {
            var result = new List<ChannelDto>();

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    try
                    {
                        var ch = item.GetProperty("channel");
                        var tvgId = ch.TryGetProperty("tvgId", out var tvgProp)
                            ? tvgProp.GetString()
                            : ch.TryGetProperty("epgId", out var epgProp)
                                ? epgProp.GetString()
                                : null;
                        result.Add(new ChannelDto
                        {
                            ChannelId = ch.GetProperty("channelId").GetInt32(),
                            TvgId = tvgId ?? ch.GetProperty("channelId").GetInt32().ToString(),
                            Name = ch.GetProperty("name").GetString() ?? "",
                            LogoUrl = ch.TryGetProperty("logoUrl", out var logo) ? logo.GetString() ?? "" : "",
                            HasArchive = ch.TryGetProperty("hasArchive", out var archive) && archive.GetBoolean()
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse channel item");
                    }
                }
            }

            return result;
        }

        public async Task<List<EpgItemDto>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false)
        {
            var now = DateTimeOffset.UtcNow;
            from ??= now.AddDays(-2);
            to ??= now.AddDays(1);
            var cacheKey = $"epg_{channelId}_{from.Value.UtcDateTime.Date:yyyyMMdd}_{to.Value.UtcDateTime.Date:yyyyMMdd}";

            await InitializeAsync();

            if (!forceRefresh && _cache.TryGetValue(cacheKey, out List<EpgItemDto>? cached))
            {
                _logger.LogDebug("Returning cached EPG for channel {ChannelId}", channelId);
                return cached!;
            }

            if (forceRefresh)
            {
                _logger.LogDebug("Force refresh requested for EPG {ChannelId}", channelId);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await EnsureAuthenticatedAsync();

                var startTime = from.Value.ToUniversalTime().ToString("yyyy-MM-ddT00:00:00.000Z");
                var endTime = to.Value.ToUniversalTime().ToString("yyyy-MM-ddT23:59:59.000Z");
                var filter = $"channel.id=={channelId} and startTime=ge={startTime} and endTime=le={endTime}";
                var uri = $"{_options.BaseUrl}/{_options.ApiVersion}/television/epg?filter={Uri.EscapeDataString(filter)}&limit=1000&offset=0&lang=CZ";

                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var epg = ParseEpg(json, channelId);

                var cacheExpiry = TimeSpan.FromMinutes(_cacheOptions.EpgExpirationMinutes);
                _cache.Set(cacheKey, epg, cacheExpiry);

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {EpgCount} EPG items for channel {ChannelId} in {Duration}ms",
                    epg.Count, channelId, stopwatch.ElapsedMilliseconds);

                return epg;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to get EPG for channel {ChannelId} after {Duration}ms",
                    channelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private List<EpgItemDto> ParseEpg(JsonDocument json, int channelId)
        {
            var result = new List<EpgItemDto>();

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("programs", out var programs))
                    {
                        foreach (var prog in programs.EnumerateArray())
                        {
                            try
                            {
                                var pr = prog.GetProperty("program");
                                result.Add(new EpgItemDto
                                {
                                    Title = pr.GetProperty("title").GetString() ?? "",
                                    Description = pr.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                                    StartTime = DateTimeOffset.FromUnixTimeMilliseconds(prog.GetProperty("startTimeUTC").GetInt64()).ToOffset(TimeSpan.Zero),
                                    EndTime = DateTimeOffset.FromUnixTimeMilliseconds(prog.GetProperty("endTimeUTC").GetInt64()).ToOffset(TimeSpan.Zero),
                                    Category = pr.TryGetProperty("programCategory", out var cat) && cat.TryGetProperty("desc", out var cdesc) ? cdesc.GetString() ?? "" : "",
                                    ChannelId = channelId,
                                    ScheduleId = prog.GetProperty("scheduleId").GetInt64()
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse EPG program item");
                            }
                        }
                    }
                }
            }

            return result;
        }

        public async Task<string?> GetStreamUrlAsync(int channelId)
        {
            var cacheKey = $"stream_{channelId}";

            await InitializeAsync();

            if (_cache.TryGetValue(cacheKey, out string? cached))
            {
                _logger.LogDebug("Returning cached stream URL for channel {ChannelId}", channelId);
                return cached;
            }

            try
            {
                await EnsureAuthenticatedAsync();

                var parameters = new Dictionary<string, string>
                {
                    {"service", "LIVE"},
                    {"name", _options.DeviceName},
                    {"devtype", _options.DeviceType},
                    {"id", channelId.ToString()},
                    {"prof", _options.Quality},
                    {"ecid", ""},
                    {"drm", "widevine"},
                    {"start", "LIVE"},
                    {"end", "END"},
                    {"device", "OTT_PC_HD_1080p_v2"}
                };

                var url = $"{_options.BaseUrl}/{_options.ApiVersion}/television/stream-url?" +
                         string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Headers.Referrer = new Uri($"{_options.BaseUrl}/");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (json.RootElement.TryGetProperty("url", out var streamUrl))
                {
                    var streamUrlString = streamUrl.GetString();

                    var cacheExpiry = TimeSpan.FromMinutes(_cacheOptions.StreamUrlExpirationMinutes);
                    _cache.Set(cacheKey, streamUrlString, cacheExpiry);

                    _logger.LogInformation("Retrieved stream URL for channel {ChannelId}", channelId);
                    return streamUrlString;
                }

                _logger.LogWarning("No stream URL in response for channel {ChannelId}", channelId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stream URL for channel {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<string?> GetCatchupStreamUrlAsync(long scheduleId)
        {
            try
            {
                await InitializeAsync();
                await EnsureAuthenticatedAsync();

                var parameters = new Dictionary<string, string>
                {
                    {"service", "ARCHIVE"},
                    {"name", _options.DeviceName},
                    {"devtype", _options.DeviceType},
                    {"id", scheduleId.ToString()},
                    {"prof", _options.Quality},
                    {"ecid", ""},
                    {"drm", "widevine"}
                };

                var url = $"{_options.BaseUrl}/{_options.ApiVersion}/television/stream-url?" +
                         string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (json.RootElement.TryGetProperty("url", out var streamUrl))
                {
                    var result = streamUrl.GetString();
                    _logger.LogInformation("Retrieved catchup stream URL for schedule {ScheduleId}", scheduleId);
                    return result;
                }

                _logger.LogWarning("No catchup stream URL in response for schedule {ScheduleId}", scheduleId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get catchup stream URL for schedule {ScheduleId}", scheduleId);
                throw;
            }
        }

        public async Task<List<DeviceInfoDto>> GetDevicesAsync()
        {
            try
            {
                await InitializeAsync();
                await EnsureAuthenticatedAsync();

                var url = $"{_options.BaseUrl}/{_options.ApiVersion}/home/my-devices";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var result = new List<DeviceInfoDto>();
                var root = json.RootElement;

                if (root.TryGetProperty("devices", out var devices))
                {
                    root = devices;
                }

                // New response format
                bool handled = false;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("thisDevice", out var thisDevice) && thisDevice.ValueKind == JsonValueKind.Object)
                    {
                        result.Add(ParseDevice(thisDevice));
                        handled = true;
                    }

                    if (root.TryGetProperty("smallScreenDevices", out var small))
                    {
                        AddDevices(result, small);
                        handled = true;
                    }

                    if (root.TryGetProperty("stbAndBigScreenDevices", out var big))
                    {
                        AddDevices(result, big);
                        handled = true;
                    }

                    if (!handled && root.TryGetProperty("items", out var inner))
                    {
                        AddDevices(result, inner);
                        handled = true;
                    }
                }

                if (!handled)
                {
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        AddDevices(result, root);
                    }
                    else
                    {
                        _logger.LogWarning("Devices response was not an array");
                    }
                }

                _logger.LogInformation("Retrieved {Count} devices", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get devices list");
                return new List<DeviceInfoDto>();
            }
        }

        private void AddDevices(List<DeviceInfoDto> target, JsonElement items)
        {
            if (items.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    target.Add(ParseDevice(item));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse device info item");
                }
            }
        }

        private static DeviceInfoDto ParseDevice(JsonElement item)
        {
            var id = item.TryGetProperty("id", out var idProp)
                ? GetElementString(idProp)
                : item.TryGetProperty("deviceId", out idProp)
                    ? GetElementString(idProp)
                    : string.Empty;
            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() :
                       item.TryGetProperty("deviceName", out nameProp) ? nameProp.GetString() : string.Empty;

            bool? current = null;
            if (item.TryGetProperty("isCurrent", out var currentProp))
                current = currentProp.GetBoolean();
            else if (item.TryGetProperty("current", out currentProp))
                current = currentProp.GetBoolean();

            return new DeviceInfoDto
            {
                Id = id ?? string.Empty,
                Name = name ?? string.Empty,
                IsCurrent = current
            };
        }

        private static string GetElementString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? string.Empty;
            return element.ToString();
        }

        public async Task<bool> DeleteDeviceAsync(string deviceId)
        {
            try
            {
                await InitializeAsync();
                await EnsureAuthenticatedAsync();

                var url = $"{_options.BaseUrl}/home/deleteDevice?id={deviceId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Deleted device {DeviceId}", deviceId);
                    return true;
                }

                _logger.LogWarning("Failed to delete device {DeviceId}: {Status}", deviceId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete device {DeviceId}", deviceId);
                return false;
            }
        }

        public async Task<TokenData?> RefreshTokensAsync(TokenData currentTokens)
        {
            try
            {
                await InitializeAsync();
                if (string.IsNullOrEmpty(currentTokens.RefreshToken))
                {
                    _logger.LogWarning("Cannot refresh tokens - no refresh token available for user: {Username}", currentTokens.Username);
                    return null;
                }

                var body = new { refreshToken = currentTokens.RefreshToken };
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/{_options.ApiVersion}/auth/refresh")
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentTokens.RefreshToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token refresh failed with status {Status} for user: {Username}",
                        response.StatusCode, currentTokens.Username);
                    return null;
                }

                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("token", out var token))
                {
                    _accessToken = token.GetProperty("accessToken").GetString();
                    _refreshToken = token.GetProperty("refreshToken").GetString();
                    _tokenExpiry = DateTime.UtcNow.AddHours(_tokenOptions.TokenExpirationHours);

                    var newTokenData = new TokenData
                    {
                        AccessToken = _accessToken ?? string.Empty,
                        RefreshToken = _refreshToken ?? string.Empty,
                        ExpiresAt = _tokenExpiry,
                        Username = currentTokens.Username,
                        DeviceId = _devId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _logger.LogInformation("Successfully refreshed tokens for user {Username}", currentTokens.Username);
                    return newTokenData;
                }

                _logger.LogWarning("Token refresh response missing token data for user: {Username}", currentTokens.Username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh tokens for user {Username}", currentTokens.Username);
                return null;
            }
        }


        private void ConfigureHttpClient()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
                _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                _logger.LogDebug("HTTP client configured with timeout: {Timeout}s, UserAgent: {UserAgent}",
                    _options.TimeoutSeconds, _options.UserAgent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to configure HTTP client headers");
            }
        }
    }
}
