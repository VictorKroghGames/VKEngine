namespace VKEngine.Graphics;

public interface ICommandPoolFactory
{
    ICommandPool CreateCommandPool();
}

public interface ICommandPool : ICommandBufferAllocator
{
    void Cleanup();
}
