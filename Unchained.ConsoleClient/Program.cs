using Terminal.Gui;
using Unchained.Client;
using Unchained.Client.Models;
using Unchained.Client.Models.Session;
using Unchained.Client.Models.Events;

Application.Init();
var top = Application.Top;

var win = new Window("Unchained Console Client")
{
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};
top.Add(win);

var statusBar = new StatusBar(new[]
{
    new StatusItem(Key.Q | Key.CtrlMask, "~CTRL-Q~ Quit", () => Application.RequestStop())
});
top.Add(statusBar);

string baseUrl = "";
PClient client = null!;
NotificationClient notifier = null!;

var _logView = new TextView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ReadOnly = true,
    WordWrap = true
};
win.Add(_logView);

void Log(string msg)
{
    Application.MainLoop.Invoke(() =>
    {
        _logView.Text = _logView.Text + $"\n{DateTime.Now:HH:mm:ss} {msg}";
        _logView.MoveEnd();
    });
}

string ShowInput(string title, string prompt)
{
    var dialog = new Dialog(title, 60, 7);
    var txt = new TextField("") { X = 1, Y = 1, Width = 55 };
    dialog.Add(new Label(prompt) { X = 1, Y = 0 }, txt);

    string result = null;

    var ok = new Button("OK") { IsDefault = true };
    ok.Clicked += () =>
    {
        result = txt.Text.ToString();
        Application.RequestStop();
    };

    var cancel = new Button("Cancel");
    cancel.Clicked += () => Application.RequestStop();

    dialog.AddButton(ok);
    dialog.AddButton(cancel);
    Application.Run(dialog);
    return result;
}

// Prompt for URL
var urlDialog = new Dialog("Enter server URL", 60, 7);
var urlInput = new TextField("http://localhost:5000/")
{
    X = 1,
    Y = 1,
    Width = Dim.Fill() - 2
};
urlDialog.Add(new Label("Base URL:") { X = 1, Y = 0 });
urlDialog.Add(urlInput);

var okBtn = new Button("OK") { IsDefault = true };
okBtn.Clicked += () =>
{
    baseUrl = urlInput.Text.ToString()!;
    client = new PClient(baseUrl);
    notifier = new NotificationClient(baseUrl);

    notifier.UserLoggedIn += evt => Log($"User logged in: {evt.Username} ({evt.SessionId})");
    notifier.UserLoggedOut += evt => Log($"User logged out: {evt.Username} ({evt.Reason})");
    notifier.TokensRefreshed += evt => Log($"Tokens refreshed: {evt.Username}, expires {evt.NewExpiryTime:u}");

    notifier.FfmpegJobCompleted += evt =>
        Log(evt.Success
            ? $"Recording {evt.JobId} completed: {evt.OutputFile}"
            : $"Recording {evt.JobId} failed: {evt.ErrorMessage}");

    notifier.WorkItemStarted += evt => Log($"Work started: {evt.WorkItemName} ({evt.WorkItemId})");
    notifier.WorkItemCompleted += evt => Log(evt.Success
        ? $"Work completed: {evt.WorkItemName} ({evt.WorkItemId})"
        : $"Work failed: {evt.WorkItemName} ({evt.WorkItemId}) - {evt.ErrorMessage}");

    notifier.ServiceHealthChanged += evt =>
        Log($"Service {evt.ServiceName} is {evt.Status} (healthy: {evt.IsHealthy})");

    notifier.ClientConnected += evt => Log($"Client connected: {evt.ConnectionId}");
    notifier.ClientDisconnected += evt => Log($"Client disconnected: {evt.ConnectionId} ({evt.Error})");

    notifier.ConnectionClosed += async err => { Log($"Connection closed: {err?.Message}"); await Task.CompletedTask; };
    notifier.Reconnected += async id => { Log($"Reconnected: {id}"); await Task.CompletedTask; };

    notifier.StartAsync();
    Application.RequestStop();
};
urlDialog.AddButton(okBtn);
Application.Run(urlDialog);

var menu = new MenuBar(new MenuBarItem[]
{
    new MenuBarItem("_Actions", new MenuItem[]
    {
        new MenuItem("_Login", "", async () =>
        {
            var user = ShowInput("Login", "Username:");
            var pass = ShowInput("Login", "Password:");
            var res = await client.LoginAsync(new LoginDto { Username = user, Password = pass, RememberMe = true });
            Log(res.Success ? "Login successful" : $"Login failed: {res.Message}");
        }),

        new MenuItem("_List Channels", "", async () =>
        {
            var result = await client.GetChannelsAsync();
            if (result.Success && result.Data != null)
            {
                foreach (var ch in result.Data)
                    Log($"Channel {ch.ChannelId}: {ch.Name}");
            }
            else
                Log($"Error: {result.Message ?? string.Join(",", result.Errors)}");
        }),

        new MenuItem("_Logout", "", async () =>
        {
            var res = await client.LogoutAsync();
            Log(res.Success ? "Logged out" : $"Logout failed: {res.Message}");
        }),

        new MenuItem("_Exit", "", () => Application.RequestStop())
    })
});
top.Add(menu);

// Start main UI loop after all Toplevels are initialized
Application.Run(top);
