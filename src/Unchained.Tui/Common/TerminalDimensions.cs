using Terminal.Gui;

namespace Unchained.Tui.Common;

internal static class TerminalDimensions
{
    public static bool EnsureMinimumSize(int minWidth, int minHeight, out string? message)
    {
        var driver = Application.Driver;
        var currentWidth = driver?.Cols ?? 0;
        var currentHeight = driver?.Rows ?? 0;
        var fits = currentWidth >= minWidth && currentHeight >= minHeight;

        message = fits
            ? null
            : $"Terminal size {currentWidth}x{currentHeight} is too small. Minimum required is {minWidth}x{minHeight}. Please resize the terminal and try again.";

        return fits;
    }

    public static (int Width, int Height) BoundToDriver(int desiredWidth, int desiredHeight, int padding = 2)
    {
        var driver = Application.Driver;
        if (driver == null)
        {
            return (desiredWidth, desiredHeight);
        }

        var maxWidth = Math.Max(1, driver.Cols - padding);
        var maxHeight = Math.Max(1, driver.Rows - padding);
        return (Math.Min(desiredWidth, maxWidth), Math.Min(desiredHeight, maxHeight));
    }
}
