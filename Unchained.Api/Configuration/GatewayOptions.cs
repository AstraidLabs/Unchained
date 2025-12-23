using System.ComponentModel.DataAnnotations;

namespace Unchained.Configuration;

public class GatewayOptions
{
    public const string SectionName = "Gateway";

    [Url]
    public string BaseUrl { get; set; } = "http://localhost:5000";

    public GatewayAuthOptions Auth { get; set; } = new();

    [Range(10, 600)]
    public int PlaylistCacheSeconds { get; set; } = 90;

    [Range(10, 600)]
    public int XmlTvCacheSeconds { get; set; } = 90;

    public bool SignalREnabled { get; set; }
    public string SignalRHubPath { get; set; } = "/hubs/status";
}

public class GatewayAuthOptions
{
    public GatewayAuthMode Mode { get; set; } = GatewayAuthMode.None;

    [StringLength(100)]
    public string ApiKeyHeader { get; set; } = "X-Api-Key";

    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;
}

public enum GatewayAuthMode
{
    None,
    ApiKey
}
