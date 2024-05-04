using Microsoft.Extensions.DependencyInjection;
using VKEngine.Graphics;
using VKEngine.Graphics.Vulkan;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddVulkanGraphics(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.Services.AddSingleton<ISwapChain, VulkanSwapChain>();
        containerBuilder.Services.AddSingleton<ICommandPoolFactory, VulkanCommandPoolFactory>();

        containerBuilder.Services.AddSingleton<IVulkanPhysicalDevice, VulkanPhysicalDevice>();
        containerBuilder.Services.AddSingleton<IVulkanLogicalDevice, VulkanLogicalDevice>();
        containerBuilder.Services.AddSingleton<IVulkanPipeline, VulkanPipeline>();

        containerBuilder.Services.AddSingleton<IShaderFactory, VulkanShaderFactory>();

        containerBuilder.Services.AddSingleton<ITestRenderer, VulkanRenderer>();
        containerBuilder.Services.AddSingleton<IGraphicsContext, VulkanGraphicsContext>();

        containerBuilder.Services.AddSingleton<IVulkanSwapChain>(x => x.GetRequiredService<ISwapChain>() as IVulkanSwapChain);
        containerBuilder.Services.AddSingleton<IVulkanCommandPoolFactory>(x => x.GetRequiredService<ICommandPoolFactory>() as IVulkanCommandPoolFactory);

        containerBuilder.Services.AddSingleton<IVulkanRenderPassFactory, VulkanRenderPassFactory>();
        return containerBuilder;
    }
}
