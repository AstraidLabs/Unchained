using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Unchained.Tui.Api;
using Unchained.Tui.Common;
using Unchained.Tui.Ui.Dialogs;

namespace Unchained.Tui.Ui;

public class LoginScreen : Window
{
    private readonly UnchainedApiClient _api;
    private readonly AppState _state;
    private readonly ILogger<LoginScreen> _logger;

    private readonly TextField _baseUrlField;
    private readonly TextField _usernameField;
    private readonly TextField _passwordField;
    private readonly ComboBox _profileCombo;
    private readonly Label _statusLabel;

    public event Action? LoginSucceeded;
    public event Action? QuitRequested;

    public LoginScreen(UnchainedApiClient api, AppState state, ILogger<LoginScreen> logger) : base("Login")
    {
        _api = api;
        _state = state;
        _logger = logger;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _baseUrlField = new TextField(_state.Options.BaseUrl) { X = 18, Y = 1, Width = 40 };
        _usernameField = new TextField("") { X = 18, Y = 3, Width = 40 };
        _passwordField = new TextField("") { X = 18, Y = 5, Width = 40, Secret = true };
        _profileCombo = new ComboBox
        {
            X = 18,
            Y = 7,
            Width = 40,
            Height = 4,
            ReadOnly = true,
            Text = _state.Options.Profile
        };
        _profileCombo.SetSource(new[] { "kodi", "generic", "tvheadend", "jellyfin" });

        var loginButton = new Button("Login")
        {
            X = 18,
            Y = 9,
            IsDefault = true
        };
        loginButton.Clicked += async () => await AttemptLoginAsync();

        var settingsButton = new Button("Settings")
        {
            X = Pos.Right(loginButton) + 2,
            Y = 9
        };
        settingsButton.Clicked += ShowSettings;

        var quitButton = new Button("Quit")
        {
            X = Pos.Right(settingsButton) + 2,
            Y = 9
        };
        quitButton.Clicked += () => QuitRequested?.Invoke();

        _statusLabel = new Label("")
        {
            X = 1,
            Y = 12,
            Width = Dim.Fill()
        };

        Add(
            new Label("Base URL:") { X = 1, Y = 1 },
            _baseUrlField,
            new Label("Username:") { X = 1, Y = 3 },
            _usernameField,
            new Label("Password:") { X = 1, Y = 5 },
            _passwordField,
            new Label("Profile:") { X = 1, Y = 7 },
            _profileCombo,
            loginButton,
            settingsButton,
            quitButton,
            _statusLabel);
    }

    private void ShowSettings()
    {
        var dialog = new SettingsDialog(_state.Options);
        Application.Run(dialog);
        var updated = dialog.ReadResult(_state.Options);
        _state.Load(updated);
        _baseUrlField.Text = updated.BaseUrl;
        _profileCombo.Text = updated.Profile;
    }

    private async Task AttemptLoginAsync()
    {
        var username = _usernameField.Text?.ToString() ?? string.Empty;
        var password = _passwordField.Text?.ToString() ?? string.Empty;
        var baseUrl = _baseUrlField.Text?.ToString() ?? string.Empty;
        var profile = _profileCombo.Text?.ToString() ?? "kodi";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(baseUrl))
        {
            SetStatus("Please enter base URL, username and password.");
            return;
        }

        _state.Update(options =>
        {
            options.BaseUrl = baseUrl;
            options.Profile = profile;
        });

        _api.ResetSession();
        SetStatus("Logging in...");

        try
        {
            var result = await _api.LoginAsync(username.Trim(), password, CancellationToken.None).ConfigureAwait(false);
            if (!result.Success || result.Data == null || !result.Data.Success)
            {
                SetStatus(result.Error?.Detail ?? result.Message ?? "Login failed");
                return;
            }

            SetStatus("Login successful");
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            SetStatus("Login failed");
        }
        finally
        {
            _passwordField.Text = string.Empty;
        }
    }

    private void SetStatus(string message)
    {
        Application.MainLoop.Invoke(() => _statusLabel.Text = message);
    }
}
