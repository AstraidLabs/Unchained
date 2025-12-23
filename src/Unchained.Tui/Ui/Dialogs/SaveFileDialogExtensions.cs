using Terminal.Gui;

namespace Unchained.Tui.Ui.Dialogs;

public static class SaveFileDialogExtensions
{
    public static string? PromptForSave(string title, string defaultName)
    {
        var dialog = new SaveDialog(title, "Select destination")
        {
            FileName = defaultName,
            DirectoryPath = Environment.CurrentDirectory
        };

        dialog.CancelButton.Text = "Cancel";
        dialog.SaveButton.Text = "Save";

        Application.Run(dialog);
        if (dialog.Canceled)
        {
            return null;
        }

        var directory = dialog.DirectoryPath?.ToString() ?? string.Empty;
        var fileName = dialog.FileName?.ToString() ?? defaultName;
        return Path.IsPathRooted(fileName) ? fileName : Path.Combine(directory, fileName);
    }
}
