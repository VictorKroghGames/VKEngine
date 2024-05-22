using Microsoft.Extensions.DependencyInjection;
using VKEngine.Platform;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddPlatformModule(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.Services.AddSingleton<IPlatformManager, PlatformManager>();

        containerBuilder.AddConfiguration(x => x.PlatformConfiguration);

        containerBuilder.Services.AddGlfwPlatformModule();

        return containerBuilder;
    }
}
