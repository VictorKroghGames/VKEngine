using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanBuffer(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ICommandPoolFactory commandPoolFactory, ICommandBufferAllocator commandBufferAllocator, ulong bufferSize, BufferUsageFlags usage, BufferMemoryPropertyFlags memoryPropertyFlags) : IBuffer
{
    internal VkBuffer buffer = VkBuffer.Null;
    internal VkDeviceMemory deviceMemory = VkDeviceMemory.Null;

    private bool useStagingBuffer = false;
    private bool useDirectUpload = false;
    private IntPtr mappedMemory = IntPtr.Zero;

    public unsafe void Initialize()
    {
        CreateBuffer(bufferSize, (VkBufferUsageFlags)usage, (VkMemoryPropertyFlags)memoryPropertyFlags, out buffer, out deviceMemory);

        useStagingBuffer = usage.HasFlag(BufferUsageFlags.TransferDst) && memoryPropertyFlags.HasFlag(BufferMemoryPropertyFlags.DeviceLocal);
        useDirectUpload = usage.HasFlag(BufferUsageFlags.UniformBuffer);

        if (useDirectUpload)
        {
            fixed (void* pMappedMemory = &mappedMemory)
            {
                vkMapMemory(logicalDevice.Device, deviceMemory, 0, bufferSize, 0, &pMappedMemory);
                mappedMemory = (IntPtr)pMappedMemory;
            }
        }
    }

    public void Cleanup()
    {
        logicalDevice.WaitIdle();

        vkDestroyBuffer(logicalDevice.Device, buffer, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, deviceMemory, IntPtr.Zero);
    }

    public unsafe void UploadData<T>(T[] data)
    {
        var size = (ulong)(data.Length * Unsafe.SizeOf<T>());
        if (size > bufferSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer size!");
        }

        if (useDirectUpload is true)
        {
            GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
            Unsafe.CopyBlock(mappedMemory.ToPointer(), gh.AddrOfPinnedObject().ToPointer(), (uint)size);
            gh.Free();

            return;
        }

        if (useStagingBuffer is true)
        {
            UploadDataUsingStagingBuffer(size, data, (data, mappedMemory) =>
            {
                GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
                Unsafe.CopyBlock(mappedMemory.ToPointer(), gh.AddrOfPinnedObject().ToPointer(), (uint)size);
                gh.Free();
            });

            return;
        }

        throw new NotImplementedException();
    }

    public unsafe void UploadData<T>(ref T data)
    {
        var size = (ulong)Unsafe.SizeOf<T>();
        if (size > bufferSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer size!");
        }

        if (useDirectUpload is true)
        {
            Unsafe.CopyBlock(mappedMemory.ToPointer(), Unsafe.AsPointer(ref data), (uint)size);

            return;
        }

        if (useStagingBuffer is true)
        {
            UploadDataUsingStagingBuffer(size, data, (data, mappedMemory) =>
            {
                Unsafe.CopyBlock(mappedMemory.ToPointer(), Unsafe.AsPointer(ref data), (uint)size);
            });

            return;
        }

        throw new NotImplementedException();
    }

    public unsafe void UploadData(uint offset, uint size, IntPtr data)
    {
        if (size > bufferSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer size!");
        }

        if (useDirectUpload is true)
        {
            var offsetMappedMemory = Unsafe.Add<IntPtr>(mappedMemory.ToPointer(), (int)offset);
            Unsafe.CopyBlock(offsetMappedMemory, data.ToPointer(), size);

            return;
        }

        if (useStagingBuffer is true)
        {
            UploadDataUsingStagingBuffer(size, data, (data, mappedMemory) =>
            {
                var offsetMappedMemory = Unsafe.Add<IntPtr>(mappedMemory.ToPointer(), (int)offset);
                Unsafe.CopyBlock(offsetMappedMemory, data.ToPointer(), size);
            });

            return;
        }
    }

    private unsafe void UploadDataUsingStagingBuffer<T>(ulong size, T data, Action<T, IntPtr> copyDataFunc)
    {
        if (useStagingBuffer is false)
        {
            return;
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out var stagingBuffer, out var stagingBufferMemory);

        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, stagingBufferMemory, 0, size, 0, &mappedMemory);

        copyDataFunc(data, (nint)mappedMemory);

        vkUnmapMemory(logicalDevice.Device, stagingBufferMemory);

        CopyBuffer(stagingBuffer, buffer, size);

        vkDestroyBuffer(logicalDevice.Device, stagingBuffer, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, stagingBufferMemory, IntPtr.Zero);
    }

    private unsafe void CopyBuffer(VkBuffer sourceBuffer, VkBuffer destinationBuffer, ulong size)
    {
        var commandPool = commandPoolFactory.CreateCommandPool(physicalDevice.QueueFamilyIndices.Transfer);

        var commandBuffer = commandBufferAllocator.AllocateCommandBuffer(commandPool: commandPool);
        if (commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
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
