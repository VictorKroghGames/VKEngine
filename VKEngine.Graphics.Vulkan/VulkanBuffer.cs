using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VKEngine.Graphics.Enumerations;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanBuffer(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ICommandPoolFactory commandPoolFactory, ICommandBufferAllocator commandBufferAllocator, ulong bufferSize, BufferUsageFlags usage) : IBuffer
{
    internal VkBuffer buffer = VkBuffer.Null;
    internal VkDeviceMemory deviceMemory = VkDeviceMemory.Null;

    public unsafe void Initialize()
    {
        CreateBuffer(bufferSize, (VkBufferUsageFlags)usage | VkBufferUsageFlags.TransferDst, VkMemoryPropertyFlags.DeviceLocal, out buffer, out deviceMemory);
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

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out var stagingBuffer, out var stagingBufferMemory);

        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, stagingBufferMemory, 0, size, 0, &mappedMemory);
        GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
        Unsafe.CopyBlock(mappedMemory, gh.AddrOfPinnedObject().ToPointer(), (uint)size);
        gh.Free();
        vkUnmapMemory(logicalDevice.Device, stagingBufferMemory);

        CopyBuffer(stagingBuffer, buffer, size);

        vkDestroyBuffer(logicalDevice.Device, stagingBuffer, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, stagingBufferMemory, IntPtr.Zero);
    }

    private unsafe void CopyBuffer(VkBuffer sourceBuffer, VkBuffer destinationBuffer, ulong size)
    {
        var commandPool = commandPoolFactory.CreateCommandPool(physicalDevice.QueueFamilyIndices.Transfer);

        var commandBuffer = commandBufferAllocator.AllocateCommandBuffer(commandPool: commandPool);
        if(commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        commandBuffer.Begin();

        var copyRegion = new VkBufferCopy
        {
            srcOffset = 0,
            dstOffset = 0,
            size = size
        };

        vkCmdCopyBuffer(vulkanCommandBuffer.commandBuffer, sourceBuffer, destinationBuffer, 1, &copyRegion);

        commandBuffer.End();

        vulkanCommandBuffer.SubmitUnsafe(logicalDevice.TransferQueue);

        commandPool.Cleanup();
    }

    private unsafe void CreateBuffer(ulong size, VkBufferUsageFlags bufferUsageFlags, VkMemoryPropertyFlags memoryPropertyFlags, out VkBuffer buffer, out VkDeviceMemory deviceMemory)
    {
        var bufferCreateInfo = VkBufferCreateInfo.New();
        bufferCreateInfo.size = size;
        bufferCreateInfo.usage = bufferUsageFlags;
        bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(logicalDevice.Device, &bufferCreateInfo, null, out buffer) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create buffer!");
        }

        vkGetBufferMemoryRequirements(logicalDevice.Device, buffer, out var memoryRequirements);

        var memoryAllocateInfo = VkMemoryAllocateInfo.New();
        memoryAllocateInfo.allocationSize = memoryRequirements.size;
        memoryAllocateInfo.memoryTypeIndex = physicalDevice.FindMemoryType(memoryRequirements.memoryTypeBits, memoryPropertyFlags);

        if (vkAllocateMemory(logicalDevice.Device, &memoryAllocateInfo, null, out deviceMemory) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate buffer memory!");
        }

        vkBindBufferMemory(logicalDevice.Device, buffer, deviceMemory, 0);
    }
}
