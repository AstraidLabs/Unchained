using Unchained.Configuration;
using SessionOptions = Unchained.Configuration.SessionOptions;

namespace Unchained.Services.Configuration;

public interface IConfigurationService
{
    UnchainedOptions Unchained { get; }
    SessionOptions Session { get; }
    CacheOptions Cache { get; }
    TokenStorageOptions TokenStorage { get; }
    CorsOptions Cors { get; }
    RateLimitOptions RateLimit { get; }
    NetworkOptions Network { get; }
    TelemetryOptions Telemetry { get; }
    BackgroundServiceOptions BackgroundServices { get; }
}
