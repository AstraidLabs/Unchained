using Unchained.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Unchained.Services;

namespace Unchained.Application.Commands;

public class DeleteDeviceCommand : IRequest<ApiResponse<string>>
{
    public string DeviceId { get; set; } = string.Empty;
}

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, ApiResponse<string>>
{
    private readonly IUnchained _magenta;
    private readonly ILogger<DeleteDeviceCommandHandler> _logger;

    public DeleteDeviceCommandHandler(IUnchained magenta, ILogger<DeleteDeviceCommandHandler> logger)
    {
        _magenta = magenta;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _magenta.InitializeAsync();
            var success = await _magenta.DeleteDeviceAsync(request.DeviceId);
            return success
                ? ApiResponse<string>.SuccessResult("Device deleted")
                : ApiResponse<string>.ErrorResult("Failed to delete device");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete device {DeviceId}", request.DeviceId);
            return ApiResponse<string>.ErrorResult("Failed to delete device");
        }
    }
}
