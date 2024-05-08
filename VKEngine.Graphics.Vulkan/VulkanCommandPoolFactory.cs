namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : ICommandPoolFactory
{
    public ICommandPool CreateCommandPool()
    {
        var commandPool = new VulkanCommandPool(physicalDevice, logicalDevice, swapChain);
        commandPool.Initialize();
        return commandPool;
    }
}
