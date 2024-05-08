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

    public IBuffer CreateIndexBuffer()
    {
        throw new NotImplementedException();
    }

    public IBuffer CreateStagingBuffer()
    {
        throw new NotImplementedException();
    }
}
