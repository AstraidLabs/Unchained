using Unchained.Tui.Api;
using Terminal.Gui;

namespace Unchained.Tui.Ui.Views;

public class ChannelDetailsView : FrameView
{
    private readonly Label _nameValue;
    private readonly Label _groupValue;
    private readonly Label _tvgValue;
    private readonly Label _idValue;
    private readonly Label _streamValue;

    public event Action<string>? CopyRequested;
    public event Action<string>? OpenRequested;

    private ChannelDto? _channel;

    public ChannelDetailsView() : base("Details")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        int row = 0;
        Add(new Label("Name:") { X = 0, Y = row });
        _nameValue = new Label(string.Empty) { X = 14, Y = row, Width = Dim.Fill() };
        row++;

        Add(new Label("Group:") { X = 0, Y = row });
        _groupValue = new Label(string.Empty) { X = 14, Y = row, Width = Dim.Fill() };
        row++;

        Add(new Label("TvgId:") { X = 0, Y = row });
        _tvgValue = new Label(string.Empty) { X = 14, Y = row, Width = Dim.Fill() };
        row++;

        Add(new Label("ChannelId:") { X = 0, Y = row });
        _idValue = new Label(string.Empty) { X = 14, Y = row, Width = Dim.Fill() };
        row++;

        Add(new Label("Stream:") { X = 0, Y = row });
        _streamValue = new Label(string.Empty) { X = 14, Y = row, Width = Dim.Fill() };
        row += 2;

        var copyName = new Button("Copy Name") { X = 0, Y = row };
        copyName.Clicked += () =>
        {
            if (!string.IsNullOrWhiteSpace(_channel?.Name))
                CopyRequested?.Invoke(_channel!.Name);
        };

        var copyId = new Button("Copy Id") { X = Pos.Right(copyName) + 2, Y = row };
        copyId.Clicked += () =>
        {
            var id = _channel?.ChannelId ?? _channel?.Id;
            if (!string.IsNullOrWhiteSpace(id))
                CopyRequested?.Invoke(id!);
        };

        var copyStream = new Button("Copy Stream Url") { X = Pos.Right(copyId) + 2, Y = row };
        copyStream.Clicked += () =>
        {
            if (!string.IsNullOrWhiteSpace(_channel?.StreamUrl))
                CopyRequested?.Invoke(_channel!.StreamUrl!);
        };

        row++;
        var openStream = new Button("Open Stream") { X = 0, Y = row };
        openStream.Clicked += () =>
        {
            if (!string.IsNullOrWhiteSpace(_channel?.StreamUrl))
                OpenRequested?.Invoke(_channel!.StreamUrl!);
        };

        Add(copyName, copyId, copyStream, openStream);
    }

    public void SetChannel(ChannelDto? channel)
    {
        _channel = channel;
        _nameValue.Text = channel?.Name ?? string.Empty;
        _groupValue.Text = channel?.GroupTitle ?? string.Empty;
        _tvgValue.Text = channel?.TvgId ?? string.Empty;
        _idValue.Text = channel?.ChannelId ?? channel?.Id ?? string.Empty;
        _streamValue.Text = channel?.StreamUrl ?? string.Empty;
    }
}
