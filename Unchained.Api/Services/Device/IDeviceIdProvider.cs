namespace Unchained.Services.Device;

public interface IDeviceIdProvider
{
    Task<string> GetDeviceIdAsync();
}
