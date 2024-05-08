namespace VKEngine.Graphics;

public interface ICommandBufferAllocator
{
    void Initialize();
    void Cleanup();

    ICommandBuffer AllocateCommandBuffer(CommandBufferLevel commandBufferLevel = CommandBufferLevel.Primary, ICommandPool? commandPool = default);
    void FreeCommandBuffer(ICommandBuffer commandBuffer);
}
