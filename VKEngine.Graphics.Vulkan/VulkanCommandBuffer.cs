using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanCommandBuffer : ICommandBuffer
{
    VkCommandBuffer Raw { get; }
}

internal sealed class VulkanCommandBuffer(VkCommandBuffer commandBuffer) : IVulkanCommandBuffer
{
    public VkCommandBuffer Raw => commandBuffer;

    public void Begin()
    {
        VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
        beginInfo.flags = VkCommandBufferUsageFlags.SimultaneousUse;
        vkBeginCommandBuffer(commandBuffer, ref beginInfo);
    }

    public void End()
    {
        vkEndCommandBuffer(commandBuffer);
    }
}
