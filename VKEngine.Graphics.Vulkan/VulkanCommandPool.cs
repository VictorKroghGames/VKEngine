using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPool(IVulkanLogicalDevice logicalDevice, uint queueFamilyIndex) : ICommandPool
{
    internal VkCommandPool commandPool;

    internal unsafe void Initialize()
    {
        var commandPoolCreateInfo = VkCommandPoolCreateInfo.New();
        commandPoolCreateInfo.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        commandPoolCreateInfo.queueFamilyIndex = queueFamilyIndex;

        if(vkCreateCommandPool(logicalDevice.Device, &commandPoolCreateInfo, null, out commandPool) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create command pool!");
        }
    }

    public void Cleanup()
    {
        vkDestroyCommandPool(logicalDevice.Device, commandPool, IntPtr.Zero);
    }
}
