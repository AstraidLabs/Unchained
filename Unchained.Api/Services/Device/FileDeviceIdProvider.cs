using Microsoft.Extensions.Logging;

namespace Unchained.Services.Device;

public class FileDeviceIdProvider : IDeviceIdProvider
{
    private readonly ILogger<FileDeviceIdProvider> _logger;
    private const string DeviceIdFile = "dev_id.txt";
    private Task<string>? _deviceIdTask;

    public FileDeviceIdProvider(ILogger<FileDeviceIdProvider> logger)
    {
        _logger = logger;
    }

    public Task<string> GetDeviceIdAsync()
    {
        _deviceIdTask ??= GetOrCreateDeviceIdAsync();
        return _deviceIdTask;
    }

    private async Task<string> GetOrCreateDeviceIdAsync()
    {
        try
        {
            if (File.Exists(DeviceIdFile))
            {
                var deviceId = (await File.ReadAllTextAsync(DeviceIdFile)).Trim();
                if (!string.IsNullOrEmpty(deviceId))
                {
                    _logger.LogDebug("Using existing device ID: {DeviceId}", deviceId);
                    return deviceId;
                }
            }

            var newDeviceId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(DeviceIdFile, newDeviceId);
            _logger.LogInformation("Created new device ID: {DeviceId}", newDeviceId);
            return newDeviceId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read/write device ID file, using temporary ID");
            return Guid.NewGuid().ToString();
        }
    }
}
