using System.Numerics;
using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;

public interface IBufferFactory
{
    IBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags memoryPropertyFlags);
    IBuffer CreateVertexBuffer(ulong bufferSize);
    IBuffer CreateIndexBuffer<T>(uint indexCount) where T : INumber<T>;
}

public interface IBuffer
{
    void Initialize();
    void Cleanup();

    void SetData<T>(T[] data);
    void SetData<T>(ref T data);
}
