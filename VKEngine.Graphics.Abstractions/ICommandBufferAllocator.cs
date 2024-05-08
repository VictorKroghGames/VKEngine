namespace VKEngine.Graphics;

public interface ICommandBufferAllocator
{
    void Initialize();
    void Cleanup();

    ICommandBuffer AllocateCommandBuffer(ICommandPool? commandPool = default);
    void FreeCommandBuffer(ICommandBuffer commandBuffer);
}
