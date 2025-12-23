using Terminal.Gui;

namespace Unchained.Tui.Common;

public class ClipboardHelper
{
    private readonly Action<string> _log;

    public ClipboardHelper(Action<string> log)
    {
        _log = log;
    }

    public bool TryCopy(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            Clipboard.TrySetClipboardData(text);
            _log($"Copied to clipboard: {text}");
            return true;
        }
        catch (Exception ex)
        {
            _log($"Clipboard unavailable: {ex.Message}");
            return false;
        }
    }
}
