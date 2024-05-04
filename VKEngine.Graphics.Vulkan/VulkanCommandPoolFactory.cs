namespace VKEngine.Graphics.Vulkan;

public interface IVulkanCommandPoolFactory
{
    IVulkanCommandPool CreateCommandPool();
}

internal sealed class VulkanCommandPoolFactory(IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : ICommandPoolFactory, IVulkanCommandPoolFactory
{
    public IVulkanCommandPool CreateCommandPool()
    {
        var commandPool = new VulkanCommandPool(vulkanPhysicalDevice, vulkanLogicalDevice);
        commandPool.Initialize();
        return commandPool;
    }

    ICommandPool ICommandPoolFactory.CreateCommandPool()
    {
        return CreateCommandPool();
    }
}
