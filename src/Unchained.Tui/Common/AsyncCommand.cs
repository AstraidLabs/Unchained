using Terminal.Gui;

namespace Unchained.Tui.Common;

public class AsyncCommand
{
    private readonly View _view;

    public AsyncCommand(View view)
    {
        _view = view;
    }

    public Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            try
            {
                Application.MainLoop.Invoke(() => _view.Enabled = false);
                await action(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Application.MainLoop.Invoke(() => _view.Enabled = true);
            }
        }, cancellationToken);
    }
}
