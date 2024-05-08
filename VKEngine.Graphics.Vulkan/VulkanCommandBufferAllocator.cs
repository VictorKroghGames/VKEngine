namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanCommandBufferAllocator(ICommandPoolFactory commandPoolFactory) : ICommandBufferAllocator
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

    public ICommandBuffer AllocateCommandBuffer(ICommandPool? commandPool = null)
    {
        var desiredCommandPool = commandPool ?? defaultCommandPool;
        if (desiredCommandPool is not VulkanCommandPool vulkanCommandPool)
        {
            throw new InvalidOperationException("Invalid command pool type!");
        }

        return vulkanCommandPool.AllocateCommandBuffer();
    }

    public void FreeCommandBuffer(ICommandBuffer commandBuffer)
    {
        throw new NotImplementedException();
    }
}
