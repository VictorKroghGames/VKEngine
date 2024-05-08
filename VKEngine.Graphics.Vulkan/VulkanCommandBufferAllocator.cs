using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandBufferAllocator(IVulkanLogicalDevice logicalDevice, ISwapChain swapChain, ICommandPoolFactory commandPoolFactory) : ICommandBufferAllocator
{
    private ICommandPool defaultCommandPool = default!;

    public void Initialize()
    {
        defaultCommandPool = commandPoolFactory.CreateCommandPool();
    }

    public void Cleanup()
    {
        defaultCommandPool.Cleanup();
    }

    public unsafe ICommandBuffer AllocateCommandBuffer(CommandBufferLevel commandBufferLevel = CommandBufferLevel.Primary, ICommandPool? commandPool = null)
    {
        var desiredCommandPool = commandPool ?? defaultCommandPool;
        if (desiredCommandPool is not VulkanCommandPool vulkanCommandPool)
        {
            throw new InvalidOperationException("Invalid command pool type!");
        }

        var commandBufferAllocateInfo = VkCommandBufferAllocateInfo.New();
        commandBufferAllocateInfo.commandPool = vulkanCommandPool.commandPool;
        commandBufferAllocateInfo.level = (VkCommandBufferLevel)commandBufferLevel;
        commandBufferAllocateInfo.commandBufferCount = 1;

        if (vkAllocateCommandBuffers(logicalDevice.Device, &commandBufferAllocateInfo, out var commandBufferHandle) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        return new VulkanCommandBuffer(vulkanCommandPool, commandBufferHandle, logicalDevice, swapChain);
    }

    public unsafe void FreeCommandBuffer(ICommandBuffer commandBuffer)
    {
        if (commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidOperationException("Invalid command buffer type!");
        }

        if (vulkanCommandBuffer.commandPool is not VulkanCommandPool vulkanCommandPool)
        {
            throw new InvalidOperationException("Invalid command pool type!");
        }

        fixed (VkCommandBuffer* commandBufferPtr = &vulkanCommandBuffer.commandBuffer)
        {
            vkFreeCommandBuffers(logicalDevice.Device, vulkanCommandPool.commandPool, 1, commandBufferPtr);
        }
    }
}
