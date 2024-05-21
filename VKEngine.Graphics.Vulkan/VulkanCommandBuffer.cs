using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandBuffer(ICommandPool commandPool, VkCommandBuffer commandBuffer, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : ICommandBuffer
{
    internal ICommandPool commandPool = commandPool;
    internal VkCommandBuffer commandBuffer = commandBuffer;

    public void Cleanup()
    {
    }

    public unsafe void Begin(CommandBufferUsageFlags flags = CommandBufferUsageFlags.None)
    {
        logicalDevice.WaitIdle();

        if (vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to reset command buffer!");
        }

        var commandBufferBeginInfo = VkCommandBufferBeginInfo.New();
        commandBufferBeginInfo.flags = (VkCommandBufferUsageFlags)flags;

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

    public unsafe void Submit()
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        var pipelineStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;

        var submitInfo = VkSubmitInfo.New();
        submitInfo.pWaitDstStageMask = &pipelineStageFlags;
        submitInfo.waitSemaphoreCount = 1;
        fixed (VkSemaphore* waitSemaphorePtr = &vulkanSwapChain.imageAvailableSemaphores[vulkanSwapChain.CurrentFrameIndex])
        {
            submitInfo.pWaitSemaphores = waitSemaphorePtr;
        }

        submitInfo.commandBufferCount = 1;
        fixed (VkCommandBuffer* commandBufferPtr = &commandBuffer)
        {
            submitInfo.pCommandBuffers = commandBufferPtr;
        }

        submitInfo.signalSemaphoreCount = 1;
        fixed (VkSemaphore* signalSemaphorePtr = &vulkanSwapChain.renderFinishedSemaphores[vulkanSwapChain.CurrentFrameIndex])
        {
            submitInfo.pSignalSemaphores = signalSemaphorePtr;
        }

        //if (vkQueueSubmit(logicalDevice.GraphicsQueue, 1, &submitInfo, vulkanSwapChain.inFlightFences[vulkanSwapChain.CurrentFrameIndex]) is not VkResult.Success)
        if (vkQueueSubmit(logicalDevice.GraphicsQueue, 1, &submitInfo, VkFence.Null) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to submit command buffer!");
        }
    }

    internal unsafe void SubmitUnsafe(VkQueue queue)
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        var submitInfo = VkSubmitInfo.New();
        submitInfo.commandBufferCount = 1;
        fixed (VkCommandBuffer* commandBufferPtr = &commandBuffer)
        {
            submitInfo.pCommandBuffers = commandBufferPtr;
        }

        if (vkQueueSubmit(queue, 1, &submitInfo, VkFence.Null) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to submit command buffer!");
        }

        vkQueueWaitIdle(queue);
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
        renderPassBeginInfo.framebuffer = vulkanSwapChain.framebuffers[vulkanSwapChain.imageIndex];
        renderPassBeginInfo.renderArea.offset = VkOffset2D.Zero;
        renderPassBeginInfo.renderArea.extent = vulkanSwapChain.extent;

        var clearValues = stackalloc VkClearValue[1];
        clearValues[0].color.float32_0 = 0.2f;
        clearValues[0].color.float32_1 = 0.4f;
        clearValues[0].color.float32_2 = 0.8f;
        clearValues[0].color.float32_3 = 1.0f;
        //clearValues[0].depthStencil.depth = 1.0f;
        //clearValues[0].depthStencil.stencil = 0xFFFFFFFF;
        renderPassBeginInfo.pClearValues = clearValues;
        renderPassBeginInfo.clearValueCount = 1;

        vkCmdBeginRenderPass(commandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
    }

    public void EndRenderPass()
    {
        vkCmdEndRenderPass(commandBuffer);
    }

    public void BindPipeline(IPipeline pipeline)
    {
        if (pipeline is not VulkanPipeline vulkanPipeline)
        {
            throw new InvalidOperationException("Invalid pipeline type!");
        }

        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, vulkanPipeline.pipeline);
    }

    public unsafe void BindVertexBuffer(IBuffer buffer)
    {
        if (buffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
        }

        var offset = 0ul;
        vkCmdBindVertexBuffers(commandBuffer, 0, 1, ref vulkanBuffer.buffer, &offset);
    }

    public void BindIndexBuffer(IBuffer buffer)
    {
        if (buffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
        }

        vkCmdBindIndexBuffer(commandBuffer, vulkanBuffer.buffer, 0, VkIndexType.Uint16);
    }

    public unsafe void BindDescriptorSet(IPipeline pipeline, IDescriptorSet descriptorSet, uint set = 0)
    {
        if (pipeline is not VulkanPipeline vulkanPipeline)
        {
            throw new InvalidOperationException("Invalid pipeline type!");
        }

        if (descriptorSet is not VulkanDescriptorSet vulkanDescriptorSet)
        {
            throw new InvalidOperationException("Invalid descriptor set type!");
        }

        vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, vulkanPipeline.pipelineLayout, set, 1, ref vulkanDescriptorSet.descriptorSet, 0, null);
    }

    public unsafe void Draw()
    {
        SetViewportAndScissor();

        vkCmdDraw(commandBuffer, 3, 1, 0, 0);
    }

    public void DrawIndex(uint indexCount)
    {
        SetViewportAndScissor();

        vkCmdDrawIndexed(commandBuffer, indexCount, 1, 0, 0, 0);
    }

    public void DrawIndexed(uint indexCount)
    {
        DrawIndexed(indexCount, 0, 0);
    }

    public void DrawIndexed(uint indexCount, uint firstIndex, int vertexOffset)
    {
        SetViewportAndScissor();

        vkCmdDrawIndexed(commandBuffer, indexCount, 1, firstIndex, vertexOffset, 0);
    }

    private unsafe void SetViewportAndScissor()
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        // https://anki3d.org/vulkan-coordinate-system/
        //const int miny = 0;
        //var flippedY = vulkanSwapChain.extent.height - miny;
        //var flippedHeight = -(vulkanSwapChain.extent.height - miny);

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
    }
}
