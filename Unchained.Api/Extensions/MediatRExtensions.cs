using Unchained.Application.Behaviors;
using MediatR;

namespace Unchained.Extensions
{
    public static class MediatRExtensions
    {
        public static IServiceCollection AddMediatRWithBehaviors(this IServiceCollection services)
        {
            // Register MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });

            // Register behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SessionValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

            return services;
        }
    }
}