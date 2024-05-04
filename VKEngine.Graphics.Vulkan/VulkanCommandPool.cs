using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanCommandPool : ICommandPool
{
    VkCommandPool Raw { get; }

    void Initialize();
}

internal sealed class VulkanCommandPool(IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanCommandPool
{
    private VkCommandPool commandPool;

    public VkCommandPool Raw => commandPool;

    public void Initialize()
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing Command Pool!");
        }

        if (vulkanLogicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Logical device must be initialized before initializing Command Pool!");
        }

        CreateCommandPoolUnsafe();
    }

    private unsafe void CreateCommandPoolUnsafe()
    {
        VkCommandPoolCreateInfo commandPoolCreateInfo = VkCommandPoolCreateInfo.New();
        commandPoolCreateInfo.queueFamilyIndex = vulkanPhysicalDevice.QueueFamilyIndices.Graphics;
        commandPoolCreateInfo.flags = VkCommandPoolCreateFlags.Transient;
        //commandPoolCreateInfo.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        var result = vkCreateCommandPool(vulkanLogicalDevice.Device, ref commandPoolCreateInfo, null, out commandPool);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create command pool!");
        }
    }

    public unsafe IEnumerable<ICommandBuffer> AllocateCommandBuffers(uint count)
    {
        if (commandPool == VkCommandPool.Null)
        {
            CreateCommandPoolUnsafe();
        }

        VkCommandBufferAllocateInfo commandBufferAllocateInfo = VkCommandBufferAllocateInfo.New();
        commandBufferAllocateInfo.commandPool = commandPool;
        commandBufferAllocateInfo.level = VkCommandBufferLevel.Primary;
        commandBufferAllocateInfo.commandBufferCount = count;

        var commandBuffers = new VkCommandBuffer[count];

        var result = vkAllocateCommandBuffers(vulkanLogicalDevice.Device, ref commandBufferAllocateInfo, out commandBuffers[0]);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to allocate command buffer(s)!");
        }

        return commandBuffers.Select(cb => new VulkanCommandBuffer(cb));
    }

    public unsafe void FreeCommandBuffers(uint count, IEnumerable<ICommandBuffer> commandBuffers)
    {
        if (commandBuffers.All(cmdBuffers => cmdBuffers is not IVulkanCommandBuffer vulkanCommandBuffer))
        {
            throw new ApplicationException("All command buffers must be of type IVulkanCommandBuffer!");
        }

        var vulkanCommandBuffers = commandBuffers.Select(cb => cb as IVulkanCommandBuffer).ToArray();

        fixed (VkCommandBuffer* commandBufferPtr = &vulkanCommandBuffers.Select(x => x!.Raw).ToArray()[0])
        {
            vkFreeCommandBuffers(vulkanLogicalDevice.Device, commandPool, count, commandBufferPtr);
        }
    }

    public void Reset()
    {
        vkResetCommandPool(vulkanLogicalDevice.Device, commandPool, VkCommandPoolResetFlags.ReleaseResources);
    }
}
