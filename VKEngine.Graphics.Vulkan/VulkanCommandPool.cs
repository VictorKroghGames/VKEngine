using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandPool(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : ICommandPool
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

    internal unsafe ICommandBuffer AllocateCommandBuffer()
    {
        var commandBufferAllocateInfo = VkCommandBufferAllocateInfo.New();
        commandBufferAllocateInfo.commandPool = commandPool;
        commandBufferAllocateInfo.level = VkCommandBufferLevel.Primary;
        commandBufferAllocateInfo.commandBufferCount = 1;

        if (vkAllocateCommandBuffers(logicalDevice.Device, &commandBufferAllocateInfo, out var commandBufferHandle) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        return new VulkanCommandBuffer(commandBufferHandle, logicalDevice, swapChain);
    }

    internal unsafe void FreeCommandBuffer(ICommandBuffer commandBuffer)
    {
        vkDeviceWaitIdle(logicalDevice.Device);

        if (commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidOperationException("Invalid command buffer type!");
        }

        var commandBufferHandle = vulkanCommandBuffer.CommandBuffer;
        vkFreeCommandBuffers(logicalDevice.Device, commandPool, 1, &commandBufferHandle);
    }
}
