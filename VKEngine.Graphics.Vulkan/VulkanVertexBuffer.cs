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

    public unsafe void SetData()
    {
        var b = vkBindBufferMemory(logicalDevice.Device, buffer, bufferMemory, 0);

        void* data = null;
        var r = vkMapMemory(logicalDevice.Device, bufferMemory, 0, bufferSize, 0, &data);
        {
            float* vertices = (float*)data;

            // Vertex 1
            vertices[0] = 0.0f;
            vertices[1] = -0.5f;
            vertices[2] = 1.0f;
            vertices[3] = 1.0f;
            vertices[4] = 1.0f;

            // Vertex 2
            vertices[5] = 0.5f;
            vertices[6] = 0.5f;
            vertices[7] = 0.0f;
            vertices[8] = 1.0f;
            vertices[9] = 0.0f;

            // Vertex 3
            vertices[10] = -0.5f;
            vertices[11] = 0.5f;
            vertices[12] = 0.0f;
            vertices[13] = 0.0f;
            vertices[14] = 1.0f;
        }
        vkUnmapMemory(logicalDevice.Device, bufferMemory);
    }
}
