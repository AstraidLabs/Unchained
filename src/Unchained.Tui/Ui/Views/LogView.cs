using Terminal.Gui;

namespace Unchained.Tui.Ui.Views;

public class LogView : FrameView
{
    private readonly TextView _textView;

    public LogView() : base("Log")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _textView = new TextView
        {
            ReadOnly = true,
            WordWrap = true,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_textView);
    }

    public void Append(string message)
    {
        Application.MainLoop.Invoke(() =>
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            var current = _textView.Text?.ToString() ?? string.Empty;
            _textView.Text = string.IsNullOrEmpty(current) ? line : current + Environment.NewLine + line;
            _textView.MoveEnd();
        });
    }

    public void Clear()
    {
        Application.MainLoop.Invoke(() => _textView.Text = string.Empty);
    }
}
