using Unchained.Models;
using Unchained.Services.TokenStorage;

namespace Unchained.Services;

public interface IUnchained
{
    /// <summary>
    /// Asynchronously initializes the service
    /// </summary>
    Task InitializeAsync();
    /// <summary>
    /// Přihlášení uživatele
    /// </summary>
    Task<bool> LoginAsync(string username, string password);

    /// <summary>
    /// Odhlášení uživatele a vymazání tokenů
    /// </summary>
    Task LogoutAsync(string sessionId);

    /// <summary>
    /// Získání seznamu kanálů
    /// </summary>
    Task<List<ChannelDto>> GetChannelsAsync(bool forceRefresh = false);

    /// <summary>
    /// Získání EPG pro kanál
    /// </summary>
    Task<List<EpgItemDto>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false);

    /// <summary>
    /// Získání stream URL pro kanál
    /// </summary>
    Task<string?> GetStreamUrlAsync(int channelId);

    /// <summary>
    /// Získání catchup stream URL
    /// </summary>
    Task<string?> GetCatchupStreamUrlAsync(long scheduleId);

    /// <summary>
    /// Získání seznamu zařízení propojených s účtem
    /// </summary>
    Task<List<DeviceInfoDto>> GetDevicesAsync();

    /// <summary>
    /// Smazání zařízení podle jeho ID
    /// </summary>
    Task<bool> DeleteDeviceAsync(string deviceId);

    /// <summary>
    /// Obnoví access token pomocí refresh tokenu
    /// </summary>
    Task<TokenData?> RefreshTokensAsync(TokenData currentTokens);
}
