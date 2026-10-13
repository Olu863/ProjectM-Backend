using Microsoft.Extensions.DependencyInjection;
using ProjectM.Services.Services;

namespace ProjectM.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
