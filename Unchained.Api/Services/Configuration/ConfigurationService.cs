using Unchained.Configuration;
using Microsoft.Extensions.Options;
using SessionOptions = Unchained.Configuration.SessionOptions;

namespace Unchained.Services.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly IOptionsMonitor<UnchainedOptions> _magentaOptions;
    private readonly IOptionsMonitor<SessionOptions> _sessionOptions;
    private readonly IOptionsMonitor<CacheOptions> _cacheOptions;
    private readonly IOptionsMonitor<TokenStorageOptions> _tokenStorageOptions;
    private readonly IOptionsMonitor<CorsOptions> _corsOptions;
    private readonly IOptionsMonitor<RateLimitOptions> _rateLimitOptions;
    private readonly IOptionsMonitor<NetworkOptions> _networkOptions;
    private readonly IOptionsMonitor<TelemetryOptions> _telemetryOptions;
    private readonly IOptionsMonitor<BackgroundServiceOptions> _backgroundServiceOptions;

    public ConfigurationService(
        IOptionsMonitor<UnchainedOptions> magentaOptions,
        IOptionsMonitor<SessionOptions> sessionOptions,
        IOptionsMonitor<CacheOptions> cacheOptions,
        IOptionsMonitor<TokenStorageOptions> tokenStorageOptions,
        IOptionsMonitor<CorsOptions> corsOptions,
        IOptionsMonitor<RateLimitOptions> rateLimitOptions,
        IOptionsMonitor<NetworkOptions> networkOptions,
        IOptionsMonitor<TelemetryOptions> telemetryOptions,
        IOptionsMonitor<BackgroundServiceOptions> backgroundServiceOptions)
    {
        _magentaOptions = magentaOptions;
        _sessionOptions = sessionOptions;
        _cacheOptions = cacheOptions;
        _tokenStorageOptions = tokenStorageOptions;
        _corsOptions = corsOptions;
        _rateLimitOptions = rateLimitOptions;
        _networkOptions = networkOptions;
        _telemetryOptions = telemetryOptions;
        _backgroundServiceOptions = backgroundServiceOptions;
    }

    public UnchainedOptions Unchained => _magentaOptions.CurrentValue;
    public SessionOptions Session => _sessionOptions.CurrentValue;
    public CacheOptions Cache => _cacheOptions.CurrentValue;
    public TokenStorageOptions TokenStorage => _tokenStorageOptions.CurrentValue;
    public CorsOptions Cors => _corsOptions.CurrentValue;
    public RateLimitOptions RateLimit => _rateLimitOptions.CurrentValue;
    public NetworkOptions Network => _networkOptions.CurrentValue;
    public TelemetryOptions Telemetry => _telemetryOptions.CurrentValue;
    public BackgroundServiceOptions BackgroundServices => _backgroundServiceOptions.CurrentValue;
}
