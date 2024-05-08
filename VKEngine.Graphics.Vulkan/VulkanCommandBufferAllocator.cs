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

    public unsafe ICommandBuffer AllocateCommandBuffer(ICommandPool? commandPool = null)
    {
        var desiredCommandPool = commandPool ?? defaultCommandPool;
        if (desiredCommandPool is not VulkanCommandPool vulkanCommandPool)
        {
            throw new InvalidOperationException("Invalid command pool type!");
        }

        var commandBufferAllocateInfo = VkCommandBufferAllocateInfo.New();
        commandBufferAllocateInfo.commandPool = vulkanCommandPool.commandPool;
        commandBufferAllocateInfo.level = VkCommandBufferLevel.Primary;
        commandBufferAllocateInfo.commandBufferCount = 1;

        if (vkAllocateCommandBuffers(logicalDevice.Device, &commandBufferAllocateInfo, out var commandBufferHandle) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        return new VulkanCommandBuffer(commandBufferHandle, logicalDevice, swapChain);
    }

    public void FreeCommandBuffer(ICommandBuffer commandBuffer)
    {
        throw new NotImplementedException();
    }
}
