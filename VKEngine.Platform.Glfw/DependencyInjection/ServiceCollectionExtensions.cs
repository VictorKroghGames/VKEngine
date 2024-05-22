using Microsoft.Extensions.DependencyInjection;
using VKEngine.Platform;
using VKEngine.Platform.Glfw;

namespace VKEngine.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGlfwPlatformModule(this IServiceCollection services)
    {
        services.AddScoped<IWindowFactory, GlfwWindowFactory>();

        services.AddScoped<IWindow, GlfwWindow>();
        services.AddScoped<IInput, GlfwInput>();

        return services;
    }
}
