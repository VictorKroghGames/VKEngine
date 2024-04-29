using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;
using VKEngine.Graphics.Vulkan;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddVulkanGraphics(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.Services.AddSingleton<IRenderer, VulkanRenderer>();

        return containerBuilder;
    }
}
