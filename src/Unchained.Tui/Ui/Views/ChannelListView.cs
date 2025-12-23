using Unchained.Tui.Api;
using Terminal.Gui;

namespace Unchained.Tui.Ui.Views;

public enum ChannelSort
{
    GroupThenName,
    Name
}

public class ChannelListView : FrameView
{
    private readonly TextField _search;
    private readonly ListView _listView;
    private List<ChannelDto> _allChannels = new();
    private List<ChannelDto> _visible = new();
    private ChannelSort _sort = ChannelSort.GroupThenName;

    public event Action<ChannelDto?>? SelectionChanged;
    public event Action<string>? FilterChanged;

    public ChannelListView() : base("Channels")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var searchLabel = new Label("Search:") { X = 0, Y = 0 };
        _search = new TextField("")
        {
            X = Pos.Right(searchLabel) + 1,
            Y = 0,
            Width = Dim.Fill()
        };

        _search.Changed += (_) => ApplyFilter();
        _search.KeyPress += e =>
        {
            if (e.KeyEvent.Key == (Key.R | Key.CtrlMask))
            {
                _search.Text = string.Empty;
                e.Handled = true;
            }
        };

        _listView = new ListView
        {
            X = 0,
            Y = Pos.Bottom(_search) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        _listView.SelectedItemChanged += _ =>
        {
            var channel = _listView.SelectedItem >= 0 && _listView.SelectedItem < _visible.Count
                ? _visible[_listView.SelectedItem]
                : null;
            SelectionChanged?.Invoke(channel);
        };

        Add(searchLabel, _search, _listView);
    }

    public void SetChannels(IEnumerable<ChannelDto> channels)
    {
        _allChannels = channels.ToList();
        ApplyFilter();
    }

    public void SetSort(ChannelSort sort)
    {
        _sort = sort;
        ApplyFilter();
    }

    public string FilterText
    {
        get => _search.Text.ToString() ?? string.Empty;
        set
        {
            _search.Text = value ?? string.Empty;
            ApplyFilter();
        }
    }

    public void FocusSearch() => _search.SetFocus();

    public void Clear()
    {
        _allChannels.Clear();
        _visible.Clear();
        _listView.SetSource(Array.Empty<string>());
    }

    private void ApplyFilter()
    {
        var selectedId = _listView.SelectedItem >= 0 && _listView.SelectedItem < _visible.Count
            ? _visible[_listView.SelectedItem].Id ?? _visible[_listView.SelectedItem].ChannelId
            : null;

        var filter = (_search.Text.ToString() ?? string.Empty).Trim();
        FilterChanged?.Invoke(filter);

        IEnumerable<ChannelDto> query = _allChannels;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.GroupTitle) && c.GroupTitle.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.TvgId) && c.TvgId.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.ChannelId) && c.ChannelId.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        query = _sort == ChannelSort.GroupThenName
            ? query.OrderBy(c => c.GroupTitle ?? string.Empty).ThenBy(c => c.Name)
            : query.OrderBy(c => c.Name);

        _visible = query.ToList();
        var items = _visible.Select(FormatChannel).ToList();
        _listView.SetSource(items);

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            var idx = _visible.FindIndex(c => c.Id == selectedId || c.ChannelId == selectedId);
            if (idx >= 0)
            {
                _listView.SelectedItem = idx;
            }
        }
    }

    private static string FormatChannel(ChannelDto channel)
    {
        var group = string.IsNullOrWhiteSpace(channel.GroupTitle) ? "(none)" : channel.GroupTitle;
        var name = string.IsNullOrWhiteSpace(channel.Name) ? "<unknown>" : channel.Name;
        return $"{group} / {name}";
    }
}
