namespace Unchained.Tui.Common;

public class AppState
{
    private readonly object _sync = new();
    public UnchainedOptions Options { get; private set; } = new();

    public event Action<UnchainedOptions>? Changed;

    public void Load(UnchainedOptions options)
    {
        lock (_sync)
        {
            Options = options;
        }
        Changed?.Invoke(Options);
    }

    public void Update(Action<UnchainedOptions> apply)
    {
        lock (_sync)
        {
            apply(Options);
        }
        Changed?.Invoke(Options);
    }
}

public class UnchainedOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Profile { get; set; } = "generic";
    public AuthOptions Auth { get; set; } = new();
    public SignalROptions SignalR { get; set; } = new();
    public HttpOptions Http { get; set; } = new();
}

public class AuthOptions
{
    public AuthMode Mode { get; set; } = AuthMode.None;
    public string ApiKeyHeader { get; set; } = "X-Api-Key";
    public string ApiKey { get; set; } = string.Empty;
}

public enum AuthMode
{
    None,
    ApiKey
}

public class SignalROptions
{
    public bool Enabled { get; set; }
    public string HubPath { get; set; } = "/hubs/status";
}

public class HttpOptions
{
    public int TimeoutSeconds { get; set; } = 30;
}
