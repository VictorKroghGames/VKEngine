using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanCommandPool
{
    VkCommandPool Raw { get; }

    void Initialize();
}

internal sealed class VulkanCommandPool(IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanCommandPool
{
    private VkCommandPool commandPool;

    public VkCommandPool Raw => commandPool;

    public void Initialize()
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing Command Pool!");
        }

        if (vulkanLogicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Logical device must be initialized before initializing Command Pool!");
        }

        CreateCommandPoolUnsafe();
    }

    private unsafe void CreateCommandPoolUnsafe()
    {
        VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
        commandPoolCI.queueFamilyIndex = vulkanPhysicalDevice.QueueFamilyIndices.Graphics;
        vkCreateCommandPool(vulkanLogicalDevice.Device, ref commandPoolCI, null, out commandPool);
    }
}
