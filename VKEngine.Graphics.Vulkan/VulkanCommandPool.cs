using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPool(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : ICommandPool
{
    internal VkCommandPool commandPool;

    internal unsafe void Initialize()
    {
        var commandPoolCreateInfo = VkCommandPoolCreateInfo.New();
        commandPoolCreateInfo.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        commandPoolCreateInfo.queueFamilyIndex = physicalDevice.QueueFamilyIndices.Graphics;

        if(vkCreateCommandPool(logicalDevice.Device, &commandPoolCreateInfo, null, out commandPool) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create command pool!");
        }
    }

    public void Cleanup()
    {
        vkDeviceWaitIdle(logicalDevice.Device);

        vkDestroyCommandPool(logicalDevice.Device, commandPool, IntPtr.Zero);
    }
}
