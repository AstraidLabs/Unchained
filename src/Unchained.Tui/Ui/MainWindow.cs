using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Unchained.Tui.Api;
using Unchained.Tui.Common;
using Unchained.Tui.SignalR;
using Unchained.Tui.Ui.Dialogs;
using Unchained.Tui.Ui.Views;

namespace Unchained.Tui.Ui;

public class MainWindow : Toplevel
{
    private readonly UnchainedApiClient _api;
    private readonly AppState _state;
    private readonly NotificationClient _notificationClient;
    private readonly ClipboardHelper _clipboard;
    private readonly ILogger<MainWindow> _logger;

    private readonly ChannelListView _channelsView;
    private readonly ChannelDetailsView _detailsView;
    private readonly LogView _logView;
    private readonly NotificationsView _notificationsView;
    private readonly Label _statusLabel;
    private readonly StatusBar _statusBar;

    private readonly List<ChannelDto> _channels = new();
    private DateTimeOffset? _lastRefresh;
    private bool _notificationsVisible;

    public MainWindow(UnchainedApiClient api, AppState state, NotificationClient notificationClient, ILogger<MainWindow> logger)
    {
        _api = api;
        _state = state;
        _notificationClient = notificationClient;
        _logger = logger;
        _clipboard = new ClipboardHelper(LogInfo);

        Title = "Unchained Gateway TUI";

        _channelsView = new ChannelListView { Width = 35, Height = Dim.Fill() };
        _detailsView = new ChannelDetailsView { X = Pos.Right(_channelsView) + 1, Width = Dim.Fill(), Height = Dim.Fill() };
        _notificationsView = new NotificationsView { Visible = false, Width = 35 };
        _logView = new LogView();
        _statusLabel = new Label(string.Empty) { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill() };

        _statusBar = new StatusBar(new[]
        {
            new StatusItem(Key.F2, "~F2~ Save M3U", async () => await SaveM3uAsync()),
            new StatusItem(Key.F3, "~F3~ Save XMLTV", async () => await SaveXmlTvAsync()),
            new StatusItem(Key.F5, "~F5~ Refresh", async () => await RefreshChannelsAsync())
        })
        {
            Visible = true
        };

        BuildLayout();
        AddMenu();
        AddKeybindings();
        HookEvents();

        Add(_statusBar);

        _state.Changed += _ => UpdateStatusLine();
        _ = AutoRefreshAsync();
        _ = EnsureNotificationsAsync();
    }

    private void BuildLayout()
    {
        var logHeight = 8;
        var statusHeight = 1;

        var mainArea = new View
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - (logHeight + statusHeight + 1)
        };

        _channelsView.Height = Dim.Fill();
        _channelsView.Width = 35;

        _notificationsView.X = Pos.AnchorEnd(_notificationsView.Width + 1);
        _notificationsView.Height = Dim.Fill();

        _detailsView.X = Pos.Right(_channelsView) + 1;
        _detailsView.Width = Dim.Fill();
        _detailsView.Height = Dim.Fill();

        mainArea.Add(_channelsView, _detailsView, _notificationsView);

        _logView.X = 0;
        _logView.Y = Pos.AnchorEnd(logHeight + statusHeight + 1);
        _logView.Width = Dim.Fill();
        _logView.Height = logHeight;

        _statusLabel.X = 1;
        _statusLabel.Y = Pos.AnchorEnd(statusHeight + 1);
        _statusLabel.Width = Dim.Fill();

        Add(mainArea, _logView, _statusLabel);
    }

    private void AddMenu()
    {
        var file = new MenuBarItem("_File", new MenuItem[]
        {
            new("_Save M3U", "", async () => await SaveM3uAsync()),
            new("_Save XMLTV", "", async () => await SaveXmlTvAsync()),
            new("_Export Channels JSON", "", async () => await ExportChannelsAsync()),
            new("_Quit", "", () => Application.RequestStop())
        });

        var view = new MenuBarItem("_View", new MenuItem[]
        {
            new("Toggle _Notifications", "", ToggleNotifications),
            new("_Clear Log", "", () => _logView.Clear())
        });

        var actions = new MenuBarItem("_Actions", new MenuItem[]
        {
            new("_Refresh", "", async () => await RefreshChannelsAsync()),
            new("_Health Live", "", async () => await CheckHealthAsync(true)),
            new("Health _Ready", "", async () => await CheckHealthAsync(false)),
            new("_Status", "", async () => await ShowStatusAsync()),
            new("Admin: Refresh _Channels", "", async () => await PostAdminAsync("/admin/refresh/channels")),
            new("Admin: Refresh _EPG", "", async () => await PostAdminAsync("/admin/refresh/epg")),
            new("Admin: Clear _Cache", "", async () => await PostAdminAsync("/admin/cache/clear"))
        });

        var settings = new MenuBarItem("_Settings", new MenuItem[]
        {
            new("_Configure", "", ShowSettings)
        });

        var menu = new MenuBar(new[] { file, view, actions, settings })
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill()
        };

        Add(menu);
    }

    private void AddKeybindings()
    {
        KeyPress += async args =>
        {
            if (args.KeyEvent.Key == (Key.F5))
            {
                await RefreshChannelsAsync();
                args.Handled = true;
            }
            else if (args.KeyEvent.Key == Key.F2)
            {
                await SaveM3uAsync();
                args.Handled = true;
            }
            else if (args.KeyEvent.Key == Key.F3)
            {
                await SaveXmlTvAsync();
                args.Handled = true;
            }
            else if (args.KeyEvent.Key == (Key.F | Key.CtrlMask))
            {
                _channelsView.FocusSearch();
                args.Handled = true;
            }
            else if (args.KeyEvent.Key == (Key.L | Key.CtrlMask))
            {
                _logView.Clear();
                args.Handled = true;
            }
        };
    }

    private void HookEvents()
    {
        _channelsView.SelectionChanged += channel => _detailsView.SetChannel(channel);
        _detailsView.CopyRequested += text => _clipboard.TryCopy(text);
        _detailsView.OpenRequested += text => TryOpenUrl(text);

        _notificationClient.EventReceived += evt =>
        {
            _notificationsView.AddEvent(evt);
            LogInfo($"Notification: {evt.Name} {evt.Content}");
        };
        _notificationClient.StatusChanged += status => LogInfo($"SignalR status: {status}");
    }

    private async Task AutoRefreshAsync()
    {
        if (Uri.TryCreate(_state.Options.BaseUrl, UriKind.Absolute, out _))
        {
            await RefreshChannelsAsync();
        }
        else
        {
            LogInfo("BaseUrl not configured. Open Settings to set BaseUrl.");
        }
    }

    private async Task EnsureNotificationsAsync()
    {
        try
        {
            await _notificationClient.StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogError("Notifications unavailable", ex);
        }
    }

    private async Task RefreshChannelsAsync()
    {
        await RunWithProgressAsync("Loading channels", async ct =>
        {
            var result = await _api.GetChannelsAsync(ct).ConfigureAwait(false);
            if (!result.Success || result.Data == null)
            {
                ShowError("Failed to load channels", result.Error, result.Message);
                return;
            }

            _channels.Clear();
            _channels.AddRange(result.Data);
            Application.MainLoop.Invoke(() => _channelsView.SetChannels(_channels));
            _lastRefresh = DateTimeOffset.Now;
            UpdateStatusLine();
            LogInfo($"Loaded {_channels.Count} channels.");
        });
    }

    private async Task SaveM3uAsync()
    {
        var path = SaveFileDialogExtensions.PromptForSave("Save M3U", "playlist.m3u");
        if (string.IsNullOrWhiteSpace(path)) return;

        await RunWithProgressAsync("Downloading M3U", async ct =>
        {
            var result = await _api.DownloadM3uAsync(_state.Options.Profile, ct).ConfigureAwait(false);
            if (!result.Success || result.Data == null)
            {
                ShowError("Unable to download M3U", result.Error, result.Message);
                return;
            }

            await File.WriteAllTextAsync(path, result.Data, ct).ConfigureAwait(false);
            LogInfo($"Saved M3U to {path}");
        });
    }

    private async Task SaveXmlTvAsync()
    {
        var path = SaveFileDialogExtensions.PromptForSave("Save XMLTV", "epg.xml");
        if (string.IsNullOrWhiteSpace(path)) return;

        var from = DateTimeOffset.Now;
        var to = from.AddDays(2);

        await RunWithProgressAsync("Downloading XMLTV", async ct =>
        {
            var result = await _api.DownloadXmlTvAsync(from, to, ct).ConfigureAwait(false);
            if (!result.Success || result.Data == null)
            {
                ShowError("Unable to download XMLTV", result.Error, result.Message);
                return;
            }

            await File.WriteAllTextAsync(path, result.Data, ct).ConfigureAwait(false);
            LogInfo($"Saved XMLTV to {path}");
        });
    }

    private async Task ExportChannelsAsync()
    {
        var path = SaveFileDialogExtensions.PromptForSave("Export Channels", "channels.json");
        if (string.IsNullOrWhiteSpace(path)) return;

        await RunWithProgressAsync("Exporting", async ct =>
        {
            var content = Formatters.ToJson(_channels);
            await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
            LogInfo($"Exported {_channels.Count} channels to {path}");
        });
    }

    private async Task CheckHealthAsync(bool live)
    {
        await RunWithProgressAsync("Health", async ct =>
        {
            var result = await _api.GetHealthAsync(live, ct).ConfigureAwait(false);
            if (!result.Success)
            {
                ShowError("Health check failed", result.Error, result.Message);
                return;
            }

            LogInfo($"Health {(live ? "live" : "ready")}: {result.Data}");
            Application.MainLoop.Invoke(() => MessageBox.Query("Health", result.Data ?? "No data", "OK"));
        });
    }

    private async Task ShowStatusAsync()
    {
        await RunWithProgressAsync("Status", async ct =>
        {
            var result = await _api.GetStatusAsync(ct).ConfigureAwait(false);
            if (!result.Success || result.Data == null)
            {
                ShowError("Status failed", result.Error, result.Message);
                return;
            }

            var statusText = Formatters.ToJson(result.Data);
            LogInfo("Status loaded");
            Application.MainLoop.Invoke(() => MessageBox.Query("Status", statusText, "OK"));
        });
    }

    private async Task PostAdminAsync(string path)
    {
        await RunWithProgressAsync("Admin", async ct =>
        {
            var result = await _api.PostAdminAsync(path, ct).ConfigureAwait(false);
            if (!result.Success)
            {
                ShowError("Admin action failed", result.Error, result.Message);
                return;
            }

            LogInfo($"Admin action {path} completed");
        });
    }

    private void ShowSettings()
    {
        var dialog = new SettingsDialog(_state.Options);
        Application.Run(dialog);
        var updated = dialog.ReadResult(_state.Options);
        _state.Load(updated);
        _ = EnsureNotificationsAsync();
        UpdateStatusLine();
    }

    private void ToggleNotifications()
    {
        _notificationsVisible = !_notificationsVisible;
        _notificationsView.Visible = _notificationsVisible;
        if (_notificationsVisible)
        {
            _notificationsView.Width = 35;
            _notificationsView.X = Pos.AnchorEnd(_notificationsView.Width + 1);
        }
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (_notificationsVisible)
        {
            _detailsView.Width = Dim.Fill() - (_notificationsView.Width + 1);
            _notificationsView.Visible = true;
        }
        else
        {
            _detailsView.Width = Dim.Fill();
            _notificationsView.Visible = false;
        }
    }

    private void UpdateStatusLine()
    {
        var options = _state.Options;
        var lastRefresh = _lastRefresh?.ToLocalTime().ToString("HH:mm:ss") ?? "never";
        var status = $"Base: {options.BaseUrl} | Profile: {options.Profile} | Channels: {_channels.Count} | Last refresh: {lastRefresh} | SignalR: {(options.SignalR.Enabled ? "on" : "off")}";
        Application.MainLoop?.Invoke(() => _statusLabel.Text = status);
    }

    private async Task RunWithProgressAsync(string title, Func<CancellationToken, Task> work)
    {
        var cts = CancellationHelper.CreateLinked(TimeSpan.FromSeconds(Math.Max(5, _state.Options.Http.TimeoutSeconds)));
        var progress = new ProgressBar { X = 1, Y = 1, Width = Dim.Fill() - 2, Height = 1, Pulse = true };
        var label = new Label("Press ESC to cancel") { X = 1, Y = 3 };

        var cancel = new Button("Cancel") { X = Pos.Center(), Y = 5 };
        cancel.Clicked += () =>
        {
            cts.Cancel();
            Application.RequestStop();
        };

        var dialog = new Dialog(title, 60, 8, cancel)
        {
            ColorScheme = Colors.Dialog
        };

        dialog.Add(progress, label);
        dialog.KeyPress += e =>
        {
            if (e.KeyEvent.Key == Key.Esc)
            {
                cts.Cancel();
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var task = Task.Run(async () =>
        {
            try
            {
                await work(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogInfo("Operation canceled");
            }
            catch (Exception ex)
            {
                LogError("Operation failed", ex);
            }
            finally
            {
                Application.MainLoop.Invoke(() => Application.RequestStop());
            }
        }, cts.Token);

        Application.Run(dialog);
        cts.Cancel();
        await task.ConfigureAwait(false);
    }

    private void ShowError(string message, ApiError? error, string? fallback)
    {
        var detail = error?.Detail ?? fallback ?? message;
        var caption = error?.Title ?? message;
        LogError(detail, null, error);
        Application.MainLoop.Invoke(() => MessageBox.ErrorQuery(caption, detail, "OK"));
    }

    private void LogError(string message, Exception? ex = null, ApiError? error = null)
    {
        var full = message;
        if (error != null)
        {
            full += $" | {error.Title} {error.Detail} {error.TraceId}";
        }

        _logger.LogError(ex, full);
        _logView.Append(full);
    }

    private void LogInfo(string message)
    {
        _logger.LogInformation(message);
        _logView.Append(message);
        UpdateStatusLine();
    }

    private void TryOpenUrl(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            LogInfo($"Opened {url}");
        }
        catch (Exception ex)
        {
            LogError("Failed to open url", ex);
        }
    }
}
