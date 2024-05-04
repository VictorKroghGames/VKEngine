namespace VKEngine.Graphics.Vulkan;

public interface IVulkanRenderPassFactory
{
    IVulkanRenderPass CreateRenderPass(IVulkanSwapChain vulkanSwapChain);
}

internal sealed class VulkanRenderPassFactory(IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanRenderPassFactory
{
    public IVulkanRenderPass CreateRenderPass(IVulkanSwapChain vulkanSwapChain)
    {
        var vulkanRenderPass = new VulkanRenderPass(vulkanLogicalDevice, vulkanSwapChain);
        vulkanRenderPass.Initialize();
        return vulkanRenderPass;
    }
}
