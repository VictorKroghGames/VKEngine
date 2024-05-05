using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;
using VKEngine.Graphics.Vulkan;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddVulkanGraphics(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.Services.AddSingleton<IGraphicsContext, VulkanGraphicsContext>();
        containerBuilder.Services.AddSingleton<IVulkanPhysicalDevice, VulkanPhysicalDevice>();
        containerBuilder.Services.AddSingleton<IVulkanLogicalDevice, VulkanLogicalDevice>();

        containerBuilder.Services.AddSingleton<ITestRenderer, VulkanTutorial>();

        return containerBuilder;
    }
}
