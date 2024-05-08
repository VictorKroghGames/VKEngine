using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanVertexBufferFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : IVertexBufferFactory
{
    public IVertexBuffer CreateVertexBuffer()
    {
        var vertexBuffer = new VulkanVertexBuffer(physicalDevice, logicalDevice);
        vertexBuffer.Initialize();
        return vertexBuffer;
    }
}

internal sealed class VulkanVertexBuffer(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : IVertexBuffer
{
    internal VkBuffer buffer = VkBuffer.Null;
    private VkDeviceMemory bufferMemory = VkDeviceMemory.Null;

    private ulong bufferSize = 3 * ((3 + 2) * sizeof(float));

    public unsafe void Initialize()
    {
        var bufferCreateInfo = VkBufferCreateInfo.New();
        bufferCreateInfo.size = bufferSize;
        bufferCreateInfo.usage = VkBufferUsageFlags.VertexBuffer;
        bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(logicalDevice.Device, &bufferCreateInfo, null, out buffer) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create vertex buffer!");
        }

        vkGetBufferMemoryRequirements(logicalDevice.Device, buffer, out var memoryRequirements);

        var memoryAllocateInfo = VkMemoryAllocateInfo.New();
        memoryAllocateInfo.allocationSize = memoryRequirements.size;
        memoryAllocateInfo.memoryTypeIndex = physicalDevice.FindMemoryType(memoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        if (vkAllocateMemory(logicalDevice.Device, &memoryAllocateInfo, null, out bufferMemory) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate vertex buffer memory!");
        }
    }

    public void Cleanup()
    {
        vkDestroyBuffer(logicalDevice.Device, buffer, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, bufferMemory, IntPtr.Zero);
    }

    public unsafe void SetData<T>(T[] data)
    {
        var size = (ulong)(data.Length * Unsafe.SizeOf<T>());
        if (size > bufferSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer size!");
        }

        vkBindBufferMemory(logicalDevice.Device, buffer, bufferMemory, 0);

        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, bufferMemory, 0, size, 0, &mappedMemory);
        GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
        Unsafe.CopyBlock(mappedMemory, gh.AddrOfPinnedObject().ToPointer(), (uint)size);
        gh.Free();
        vkUnmapMemory(logicalDevice.Device, bufferMemory);
    }
}
