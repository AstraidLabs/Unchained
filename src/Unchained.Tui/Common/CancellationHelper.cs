namespace Unchained.Tui.Common;

public static class CancellationHelper
{
    public static CancellationTokenSource CreateLinked(TimeSpan timeout, CancellationToken external = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
        if (timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(timeout);
        }
        return cts;
    }
}
