using Unchained.Tui.SignalR;
using Terminal.Gui;

namespace Unchained.Tui.Ui.Views;

public class NotificationsView : FrameView
{
    private readonly List<NotificationEvent> _events = new();
    private readonly ListView _listView;

    public NotificationsView() : base("Notifications")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _listView = new ListView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_listView);
    }

    public void AddEvent(NotificationEvent evt)
    {
        Application.MainLoop.Invoke(() =>
        {
            _events.Insert(0, evt);
            if (_events.Count > 200)
            {
                _events.RemoveAt(_events.Count - 1);
            }

            _listView.SetSource(_events.Select(FormatEvent).ToList());
        });
    }

    public void Clear()
    {
        Application.MainLoop.Invoke(() =>
        {
            _events.Clear();
            _listView.SetSource(Array.Empty<string>());
        });
    }

    private static string FormatEvent(NotificationEvent evt)
    {
        return $"[{evt.Timestamp:HH:mm:ss}] {evt.Name}: {evt.Content}";
    }
}
