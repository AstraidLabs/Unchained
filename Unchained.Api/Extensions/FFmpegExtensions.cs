using FFMpegCore;
using Unchained.Configuration;
using Unchained.Services.Ffmpeg;

namespace Unchained.Extensions;

public static class FFmpegExtensions
{
    public static IServiceCollection AddFfmpeg(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FFmpegOptions>(configuration.GetSection(FFmpegOptions.SectionName));

        var options = configuration.GetSection(FFmpegOptions.SectionName).Get<FFmpegOptions>() ?? new FFmpegOptions();
        GlobalFFOptions.Configure(new FFOptions
        {
            BinaryFolder = options.BinaryFolder,
            TemporaryFilesFolder = options.TemporaryFilesFolder
        });

        services.AddSingleton<IFFmpegService, FFmpegService>();

        return services;
    }
}
