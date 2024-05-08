using System.Numerics;
using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;

public interface IBufferFactory
{
    IBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags bufferMemoryPropertyFlags);
    IBuffer CreateVertexBuffer(ulong bufferSize, BufferMemoryPropertyFlags bufferMemoryPropertyFlags);
    IBuffer CreateIndexBuffer<T>(uint indexCount, BufferMemoryPropertyFlags bufferMemoryPropertyFlags) where T : INumber<T>;
}

public interface IBuffer
{
    void Initialize();
    void Cleanup();

    void SetData<T>(T[] data);
}
