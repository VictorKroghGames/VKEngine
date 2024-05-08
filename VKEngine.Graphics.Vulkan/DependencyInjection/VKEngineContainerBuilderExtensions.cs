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
        containerBuilder.Services.AddSingleton<ISwapChain, VulkanSwapChain>();

        containerBuilder.Services.AddSingleton<ICommandPoolFactory, VulkanCommandPoolFactory>();
        containerBuilder.Services.AddSingleton<ICommandBufferAllocator, VulkanCommandBufferAllocator>();

        containerBuilder.Services.AddSingleton<IShaderFactory, VulkanShaderFactory>();
        containerBuilder.Services.AddSingleton<IRenderPassFactory, VulkanRenderPassFactory>();
        containerBuilder.Services.AddSingleton<IPipelineFactory, VulkanPipelineFactory>();
        containerBuilder.Services.AddSingleton<IVertexBufferFactory, VulkanVertexBufferFactory>();

        containerBuilder.Services.AddSingleton<ITestRenderer, VulkanTutorial>();

        return containerBuilder;
    }
}
