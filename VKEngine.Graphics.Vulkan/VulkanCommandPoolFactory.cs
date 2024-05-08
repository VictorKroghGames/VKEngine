namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : ICommandPoolFactory
{
    public ICommandPool CreateCommandPool()
    {
        return CreateCommandPool(physicalDevice.QueueFamilyIndices.Graphics);
    }

    public ICommandPool CreateCommandPool(uint queueIndex)
    {
        var commandPool = new VulkanCommandPool(logicalDevice, queueIndex);
        commandPool.Initialize();
        return commandPool;
    }
}
