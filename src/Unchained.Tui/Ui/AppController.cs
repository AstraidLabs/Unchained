using Microsoft.Extensions.Logging;
using System.Linq;
using Terminal.Gui;
using Unchained.Tui.Api;
using Unchained.Tui.Common;

namespace Unchained.Tui.Ui;

public class AppController
{
    private readonly LoginScreen _loginScreen;
    private readonly MainWindow _mainWindow;
    private readonly UnchainedApiClient _apiClient;
    private readonly AppState _state;
    private readonly ILogger<AppController> _logger;
    private Toplevel? _top;

    public AppController(
        LoginScreen loginScreen,
        MainWindow mainWindow,
        UnchainedApiClient apiClient,
        AppState state,
        ILogger<AppController> logger)
    {
        _loginScreen = loginScreen;
        _mainWindow = mainWindow;
        _apiClient = apiClient;
        _state = state;
        _logger = logger;

        _loginScreen.LoginSucceeded += async () => await OnLoggedInAsync();
        _loginScreen.QuitRequested += () => Application.RequestStop();
        _mainWindow.LogoutRequested += async () => await LogoutAsync();
        _mainWindow.Unauthorized += async () => await ReturnToLoginAsync("Session expired. Please log in again.");
        _apiClient.Unauthorized += async () => await ReturnToLoginAsync("Session expired. Please log in again.");
    }

    public void Start(Toplevel top)
    {
        _top = top;
        ShowLogin();
    }

    private void ShowLogin()
    {
        Application.MainLoop.Invoke(() =>
        {
            if (_top != null)
            {
                foreach (var view in _top.Subviews.ToList())
                {
                    _top.Remove(view);
                }
            }
            _loginScreen.X = 0;
            _loginScreen.Y = 0;
            _loginScreen.Width = Dim.Fill();
            _loginScreen.Height = Dim.Fill();
            _top?.Add(_loginScreen);
        });
    }

    private void ShowMain()
    {
        Application.MainLoop.Invoke(() =>
        {
            if (_top != null)
            {
                foreach (var view in _top.Subviews.ToList())
                {
                    _top.Remove(view);
                }
            }
            _mainWindow.X = 0;
            _mainWindow.Y = 0;
            _mainWindow.Width = Dim.Fill();
            _mainWindow.Height = Dim.Fill();
            _top?.Add(_mainWindow);
        });
    }

    private async Task OnLoggedInAsync()
    {
        _logger.LogInformation("Login succeeded, opening main window");
        ShowMain();
        await _mainWindow.OnSessionStartedAsync();
    }

    private async Task LogoutAsync()
    {
        try
        {
            var result = await _apiClient.LogoutAsync(CancellationToken.None).ConfigureAwait(false);
            _logger.LogInformation("Logout response: {Success}", result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logout failed");
        }
        finally
        {
            _apiClient.ResetSession();
            await ReturnToLoginAsync("Logged out.");
        }
    }

    private async Task ReturnToLoginAsync(string? message)
    {
        _apiClient.ResetSession();
        ShowLogin();
        if (!string.IsNullOrWhiteSpace(message))
        {
            await Task.Delay(50);
            Application.MainLoop?.Invoke(() => MessageBox.Query("Session", message, "OK"));
        }
    }
}
