using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddGraphicsModule(this IVKEngineContainerBuilder containerBuilder)
    {
        return containerBuilder.AddGraphicsModule<DefaultRenderer>();
    }

    public static IVKEngineContainerBuilder AddGraphicsModule<TRenderer>(this IVKEngineContainerBuilder containerBuilder)
        where TRenderer : class, IRenderer
    {
        containerBuilder.AddConfiguration(x => x.GraphicsConfiguration);

        containerBuilder.AddVulkanGraphics();

        containerBuilder.Services.AddSingleton<IShaderLibrary, ShaderLibrary>();
        containerBuilder.Services.AddSingleton<IRenderer, TRenderer>();

        return containerBuilder;
    }
}
