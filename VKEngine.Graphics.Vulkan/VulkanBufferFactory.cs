using System.Numerics;
using System.Runtime.CompilerServices;
using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanBufferFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ICommandPoolFactory commandPoolFactory, ICommandBufferAllocator commandBufferAllocator) : IBufferFactory
{
    public IBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usage)
    {
        var buffer = new VulkanBuffer(physicalDevice, logicalDevice, commandPoolFactory, commandBufferAllocator, bufferSize, usage);
        buffer.Initialize();
        return buffer;
    }

    public IBuffer CreateVertexBuffer(ulong bufferSize)
    {
        return CreateBuffer(bufferSize, BufferUsageFlags.VertexBuffer);
    }

    public IBuffer CreateIndexBuffer<T>(uint indexCount) where T : INumber<T>
    {
        var bufferSize = (ulong)(indexCount * Unsafe.SizeOf<T>());

        return CreateBuffer(bufferSize, BufferUsageFlags.IndexBuffer);
    }
}
