namespace VKEngine.Graphics.Vulkan;

public interface IVulkanCommandPoolFactory
{
    IVulkanCommandPool CreateCommandPool();
}

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanCommandPoolFactory
{
    public IVulkanCommandPool CreateCommandPool()
    {
        var commandPool = new VulkanCommandPool(vulkanPhysicalDevice, vulkanLogicalDevice);
        commandPool.Initialize();
        return commandPool;
    }
}
