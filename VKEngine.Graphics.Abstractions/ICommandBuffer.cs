namespace VKEngine.Graphics;

public interface ICommandBuffer
{
    void Begin();
    void End();
}

public interface ICommandBufferAllocator
{
    IEnumerable<ICommandBuffer> AllocateCommandBuffers(uint count);
    void FreeCommandBuffers(uint count, IEnumerable<ICommandBuffer> commandBuffers);
}

public interface ICommandPool : ICommandBufferAllocator
{
    void Reset();
}

public interface ICommandPoolFactory
{
    ICommandPool CreateCommandPool();
}
