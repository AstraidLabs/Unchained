using Unchained.Models;
using MediatR;
using Unchained.Services;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.Queries;

public class GetDevicesQuery : IRequest<ApiResponse<List<DeviceInfoDto>>>
{
}

