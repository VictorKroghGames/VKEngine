using Microsoft.Extensions.DependencyInjection;
using VKEngine.Platform;
using VKEngine.Platform.Glfw;

namespace VKEngine.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGlfwPlatformModule(this IServiceCollection services)
    {
        services.AddScoped<IWindow, GlfwWindow>();

        return services;
    }
}
