namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : ICommandPoolFactory
{
    public ICommandPool CreateCommandPool()
    {
        return CreateCommandPool(physicalDevice.QueueFamilyIndices.Graphics);
    }

    public ICommandPool CreateCommandPool(uint queueFamilyIndex)
    {
        var commandPool = new VulkanCommandPool(logicalDevice, queueFamilyIndex);
        commandPool.Initialize();
        return commandPool;
    }
}
