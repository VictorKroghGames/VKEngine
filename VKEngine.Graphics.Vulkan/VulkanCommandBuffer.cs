using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandBuffer(VkCommandBuffer commandBuffer, ISwapChain swapChain) : ICommandBuffer
{
    internal VkCommandBuffer CommandBuffer => commandBuffer;

    public void Cleanup()
    {
    }

    public unsafe void Begin()
    {
        var commandBufferBeginInfo = VkCommandBufferBeginInfo.New();
        commandBufferBeginInfo.flags = VkCommandBufferUsageFlags.None;

        if (vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to begin command buffer!");
        }
    }

    public void End()
    {
        if (vkEndCommandBuffer(commandBuffer) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to end command buffer!");
        }
    }

    public unsafe void BeginRenderPass(IRenderPass renderPass)
    {
        if (renderPass is not VulkanRenderPass vulkanRenderPass)
        {
            throw new InvalidOperationException("Invalid render pass type!");
        }

        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        var renderPassBeginInfo = VkRenderPassBeginInfo.New();
        renderPassBeginInfo.renderPass = vulkanRenderPass.renderPass;
        renderPassBeginInfo.framebuffer = vulkanSwapChain.framebuffers[0];
        renderPassBeginInfo.renderArea.offset = VkOffset2D.Zero;
        renderPassBeginInfo.renderArea.extent = vulkanSwapChain.extent;

        var clearValues = stackalloc VkClearValue[1];
        clearValues[0].color.float32_0 = 0.0f;
        clearValues[0].color.float32_1 = 0.0f;
        clearValues[0].color.float32_2 = 0.0f;
        clearValues[0].color.float32_3 = 1.0f;
        renderPassBeginInfo.pClearValues = clearValues;
        renderPassBeginInfo.clearValueCount = 1;

        vkCmdBeginRenderPass(CommandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
    }

    public void EndRenderPass()
    {
        vkCmdEndRenderPass(CommandBuffer);
    }

    public void BindPipeline(IPipeline pipeline)
    {
        if (pipeline is not VulkanPipeline vulkanPipeline)
        {
            throw new InvalidOperationException("Invalid pipeline type!");
        }

        vkCmdBindPipeline(CommandBuffer, VkPipelineBindPoint.Graphics, vulkanPipeline.pipeline);
    }

    public unsafe void Draw()
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        var viewport = new VkViewport
        {
            x = 0.0f,
            y = 0.0f,
            width = vulkanSwapChain.extent.width,
            height = vulkanSwapChain.extent.height,
            minDepth = 0.0f,
            maxDepth = 1.0f
        };
        vkCmdSetViewport(commandBuffer, 0, 1, &viewport);

        var scissor = new VkRect2D
        {
            offset = VkOffset2D.Zero,
            extent = vulkanSwapChain.extent
        };
        vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

        vkCmdDraw(commandBuffer, 3, 1, 0, 0);
    }
}
