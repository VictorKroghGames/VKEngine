using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;
using VKEngine.Graphics.Vulkan;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddVulkanGraphics(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.Services.AddSingleton<IVulkanPhysicalDevice, VulkanPhysicalDevice>();
        containerBuilder.Services.AddSingleton<IVulkanLogicalDevice, VulkanLogicalDevice>();
        containerBuilder.Services.AddSingleton<IVulkanSwapChain, VulkanSwapChain>();
        containerBuilder.Services.AddSingleton<IVulkanCommandPool, VulkanCommandPool>();
        containerBuilder.Services.AddSingleton<IVulkanPipeline, VulkanPipeline>();

        containerBuilder.Services.AddSingleton<IShaderFactory, VulkanShaderFactory>();

        containerBuilder.Services.AddSingleton<ITestRenderer, VulkanRenderer>();
        containerBuilder.Services.AddSingleton<IGraphicsContext, VulkanGraphicsContext>();

        containerBuilder.Services.AddSingleton<IVulkanSwapChain, VulkanSwapChain>();

        return containerBuilder;
    }
}
