namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : ICommandPoolFactory
{
    public ICommandPool CreateCommandPool()
    {
        var commandPool = new VulkanCommandPool(physicalDevice, logicalDevice);
        commandPool.Initialize();
        return commandPool;
    }
}
