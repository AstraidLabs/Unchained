using System.Collections.Generic;
using Unchained.Client.Models;
using Unchained.Client.Models.Session;

namespace Unchained.Client;

public interface IPClient
{
    Task<ApiResponse<SessionCreatedDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<string>> LogoutAsync();
    Task<ApiResponse<AuthStatusDto>> GetAuthStatusAsync();
    Task<ApiResponse<List<ChannelDto>>> GetChannelsAsync(bool refresh = false);
    Task<ApiResponse<List<ChannelDto>>> GetChannelsBulkAsync(IEnumerable<int> channelIds);
    Task<ApiResponse<List<EpgItemDto>>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool refresh = false);
    Task<ApiResponse<Dictionary<int, List<EpgItemDto>>>> GetEpgBulkAsync(IEnumerable<int> channelIds, DateTimeOffset? from = null, DateTimeOffset? to = null, bool refresh = false);
    Task<ApiResponse<StreamUrlDto>> GetStreamUrlAsync(int channelId);
    Task<ApiResponse<Dictionary<int, string?>>> GetStreamUrlsBulkAsync(IEnumerable<int> channelIds);
    Task<ApiResponse<StreamUrlDto>> GetCatchupStreamAsync(long scheduleId);
    Task<ApiResponse<string>> StartRecordingAsync(int channelId, int minutes = 60);
    Task<ApiResponse<FfmpegJobStatus>> GetRecordingStatusAsync(string jobId);
    Task<ApiResponse<string>> GetPlaylistAsync();
    Task<ApiResponse<string>> GetEpgXmlAsync(int channelId);
    Task<ApiResponse<PingResultDto>> PingAsync();
    Task<ApiResponse<List<DeviceInfoDto>>> GetDevicesAsync();
    Task<ApiResponse<string>> DeleteDeviceAsync(string id);
    Task<ApiResponse<SessionInfoDto>> GetCurrentSessionAsync();
    Task<ApiResponse<List<SessionDto>>> GetUserSessionsAsync();
    Task<ApiResponse<string>> RevokeSessionAsync(string sessionId);
    Task<ApiResponse<string>> LogoutAllOtherSessionsAsync();
    Task<ApiResponse<SessionStatistics>> GetSessionStatisticsAsync();
}
