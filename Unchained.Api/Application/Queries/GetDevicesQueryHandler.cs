using MediatR;
using Unchained.Models;
using Unchained.Services;

namespace Unchained.Application.Queries
{
    public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, ApiResponse<List<DeviceInfoDto>>>
    {
        private readonly IUnchained _magenta;
        private readonly ILogger<GetDevicesQueryHandler> _logger;

        public GetDevicesQueryHandler(IUnchained magenta, ILogger<GetDevicesQueryHandler> logger)
        {
            _magenta = magenta;
            _logger = logger;
        }

        public async Task<ApiResponse<List<DeviceInfoDto>>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                await _magenta.InitializeAsync();
                var devices = await _magenta.GetDevicesAsync();
                return ApiResponse<List<DeviceInfoDto>>.SuccessResult(devices, $"Found {devices.Count} devices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get devices");
                return ApiResponse<List<DeviceInfoDto>>.ErrorResult("Failed to get devices");
            }
        }
    }
}
