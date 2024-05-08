using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;

public interface IBufferFactory
{
    IBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags bufferMemoryPropertyFlags);
    IBuffer CreateVertexBuffer(ulong bufferSize, BufferMemoryPropertyFlags bufferMemoryPropertyFlags);
    IBuffer CreateIndexBuffer();
    IBuffer CreateStagingBuffer();
}

public interface IBuffer
{
    void Initialize();
    void Cleanup();

    void SetData<T>(T[] data);
}
