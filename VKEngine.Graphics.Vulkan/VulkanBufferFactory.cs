using System.Numerics;
using System.Runtime.CompilerServices;
using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanBufferFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : IBufferFactory
{
    public IBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags bufferMemoryPropertyFlags)
    {
        var buffer = new VulkanBuffer(physicalDevice, logicalDevice, bufferSize, usage, bufferMemoryPropertyFlags);
        buffer.Initialize();
        return buffer;
    }

    public IBuffer CreateVertexBuffer(ulong bufferSize, BufferMemoryPropertyFlags bufferMemoryPropertyFlags)
    {
        return CreateBuffer(bufferSize, BufferUsageFlags.VertexBuffer, bufferMemoryPropertyFlags);
    }

    public IBuffer CreateIndexBuffer<T>(uint indexCount, BufferMemoryPropertyFlags bufferMemoryPropertyFlags) where T : INumber<T>
    {
        var bufferSize = (ulong)(indexCount * Unsafe.SizeOf<T>());

        return CreateBuffer(bufferSize, BufferUsageFlags.IndexBuffer, bufferMemoryPropertyFlags);
    }

    public IBuffer CreateStagingBuffer()
    {
        throw new NotImplementedException();
    }
}
