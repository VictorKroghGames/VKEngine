namespace VKEngine.Graphics.Vulkan;

public interface IVulkanRenderPassFactory
{
    IVulkanRenderPass CreateRenderPass();
}

internal sealed class VulkanRenderPassFactory(IVulkanLogicalDevice vulkanLogicalDevice, IVulkanSwapChain vulkanSwapChain) : IVulkanRenderPassFactory
{
    public IVulkanRenderPass CreateRenderPass()
    {
        var vulkanRenderPass = new VulkanRenderPass(vulkanLogicalDevice, vulkanSwapChain);
        vulkanRenderPass.Initialize();
        return vulkanRenderPass;
    }
}
