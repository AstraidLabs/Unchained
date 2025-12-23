using Unchained.Models;
using Unchained.Domain;

namespace Unchained.Services.Channels;

public interface IChannelService
{
    Task<IReadOnlyCollection<Channel>> GetChannelsAsync(bool forceRefresh = false);
}
