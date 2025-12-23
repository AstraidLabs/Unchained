namespace Unchained.Models;

public class GatewayChannelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GroupTitle { get; set; } = string.Empty;
    public string TvgId { get; set; } = string.Empty;
    public string TvgName { get; set; } = string.Empty;
    public string TvgLogo { get; set; } = string.Empty;
    public bool HasArchive { get; set; }
}
