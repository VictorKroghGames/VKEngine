using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddGraphicsModule(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.AddVulkanGraphics();

        containerBuilder.Services.AddSingleton<IShaderLibrary, ShaderLibrary>();

        return containerBuilder;
    }
}
