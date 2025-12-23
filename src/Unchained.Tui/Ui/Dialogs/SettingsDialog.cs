using Unchained.Tui.Common;
using Terminal.Gui;

namespace Unchained.Tui.Ui.Dialogs;

public class SettingsDialog : Dialog
{
    private readonly TextField _baseUrl;
    private readonly ComboBox _profile;
    private readonly CheckBox _signalREnabled;
    private readonly TextField _hubPath;

    public SettingsDialog(UnchainedOptions options) : base("Settings", 70, 20)
    {
        var baseLabel = new Label("Base Url:") { X = 1, Y = 1 };
        _baseUrl = new TextField(options.BaseUrl ?? string.Empty)
        {
            X = 1,
            Y = Pos.Bottom(baseLabel),
            Width = Dim.Fill() - 2
        };

        var profileLabel = new Label("Profile:") { X = 1, Y = Pos.Bottom(_baseUrl) + 1 };
        _profile = new ComboBox
        {
            X = 1,
            Y = Pos.Bottom(profileLabel),
            Width = 20,
            ReadOnly = true,
            Text = options.Profile
        };
        _profile.SetSource(new[] { "generic", "kodi", "tvheadend", "jellyfin" });

        var signalRLabel = new Label("SignalR:") { X = 1, Y = Pos.Bottom(_profile) + 1 };
        _signalREnabled = new CheckBox("Enable notifications", options.SignalR.Enabled)
        {
            X = 1,
            Y = Pos.Bottom(signalRLabel)
        };

        var hubLabel = new Label("Hub Path:") { X = 1, Y = Pos.Bottom(_signalREnabled) + 1 };
        _hubPath = new TextField(options.SignalR.HubPath ?? "/hubs/status")
        {
            X = 1,
            Y = Pos.Bottom(hubLabel),
            Width = Dim.Fill() - 2
        };

        Add(baseLabel, _baseUrl, profileLabel, _profile, signalRLabel, _signalREnabled, hubLabel, _hubPath);

        var ok = new Button("OK") { IsDefault = true };
        ok.Clicked += () => Application.RequestStop();

        var cancel = new Button("Cancel") { IsDefault = false };
        cancel.Clicked += () =>
        {
            _baseUrl.Text = options.BaseUrl;
            Application.RequestStop();
        };

        AddButton(ok);
        AddButton(cancel);
    }

    public UnchainedOptions ReadResult(UnchainedOptions original)
    {
        var copy = new UnchainedOptions
        {
            BaseUrl = _baseUrl.Text.ToString() ?? original.BaseUrl,
            Profile = _profile.Text.ToString() ?? original.Profile,
            Auth = new AuthOptions { CookieName = original.Auth.CookieName },
            SignalR = new SignalROptions
            {
                Enabled = _signalREnabled.Checked,
                HubPath = _hubPath.Text.ToString() ?? original.SignalR.HubPath
            },
            Http = original.Http
        };

        return copy;
    }
}
