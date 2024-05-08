using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VKEngine.Graphics.Enumerations;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanBuffer(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags bufferMemoryPropertyFlags) : IBuffer
{
    internal VkBuffer buffer = VkBuffer.Null;
    internal VkDeviceMemory deviceMemory = VkDeviceMemory.Null;

    public unsafe void Initialize()
    {
        var bufferCreateInfo = VkBufferCreateInfo.New();
        bufferCreateInfo.size = bufferSize;
        bufferCreateInfo.usage = (VkBufferUsageFlags)usage;
        bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(logicalDevice.Device, &bufferCreateInfo, null, out buffer) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create buffer!");
        }

        vkGetBufferMemoryRequirements(logicalDevice.Device, buffer, out var memoryRequirements);

        var memoryAllocateInfo = VkMemoryAllocateInfo.New();
        memoryAllocateInfo.allocationSize = memoryRequirements.size;
        memoryAllocateInfo.memoryTypeIndex = physicalDevice.FindMemoryType(memoryRequirements.memoryTypeBits, (VkMemoryPropertyFlags)bufferMemoryPropertyFlags);

        if (vkAllocateMemory(logicalDevice.Device, &memoryAllocateInfo, null, out deviceMemory) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate buffer memory!");
        }
    }

    public void Cleanup()
    {
        vkDestroyBuffer(logicalDevice.Device, buffer, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, deviceMemory, IntPtr.Zero);
    }

    public unsafe void SetData<T>(T[] data)
    {
        var size = (ulong)(data.Length * Unsafe.SizeOf<T>());
        if (size > bufferSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer size!");
        }

        vkBindBufferMemory(logicalDevice.Device, buffer, deviceMemory, 0);

        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, deviceMemory, 0, size, 0, &mappedMemory);
        GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
        Unsafe.CopyBlock(mappedMemory, gh.AddrOfPinnedObject().ToPointer(), (uint)size);
        gh.Free();
        vkUnmapMemory(logicalDevice.Device, deviceMemory);
    }
}
