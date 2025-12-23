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
            Options = Normalize(options);
        }
        Changed?.Invoke(Options);
    }

    public void Update(Action<UnchainedOptions> apply)
    {
        lock (_sync)
        {
            apply(Options);
            Options = Normalize(Options);
        }
        Changed?.Invoke(Options);
    }

    private static UnchainedOptions Normalize(UnchainedOptions options)
    {
        options.BaseUrl = NormalizeBaseUrl(options.BaseUrl);
        options.Profile = string.IsNullOrWhiteSpace(options.Profile) ? "kodi" : options.Profile.Trim();
        return options;
    }

    public static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');
        return $"{trimmed}/";
    }
}

public class UnchainedOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Profile { get; set; } = "kodi";
    public AuthOptions Auth { get; set; } = new();
    public SignalROptions SignalR { get; set; } = new();
    public HttpOptions Http { get; set; } = new();
}

public class AuthOptions
{
    public string CookieName { get; set; } = "Unchained.Session";
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
