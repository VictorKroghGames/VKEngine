using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanDescriptorSetFactory(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSetFactory
{
    public IDescriptorSet CreateDescriptorSet<T>(IBuffer buffer)
    {
        if (buffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
        }

        var descriptorSet = new VulkanDescriptorSet(graphicsConfiguration, logicalDevice);
        descriptorSet.Initialize<T>(vulkanBuffer);
        return descriptorSet;
    }
}

internal sealed class VulkanDescriptorSet(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSet
{
    private VkDescriptorPool descriptorPool = VkDescriptorPool.Null;
    internal VkDescriptorSet descriptorSet = VkDescriptorSet.Null;
    internal VkDescriptorSetLayout descriptorSetLayout = VkDescriptorSetLayout.Null;

    internal unsafe void Initialize<T>(VulkanBuffer vulkanBuffer)
    {
        var descriptorSetLayoutBinding = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex,
            pImmutableSamplers = null
        };

        var descriptorSetLayoutCreateInfo = VkDescriptorSetLayoutCreateInfo.New();
        descriptorSetLayoutCreateInfo.bindingCount = 1;
        descriptorSetLayoutCreateInfo.pBindings = &descriptorSetLayoutBinding;

        if (vkCreateDescriptorSetLayout(logicalDevice.Device, &descriptorSetLayoutCreateInfo, null, out descriptorSetLayout) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create descriptor set layout!");
        }

        VkDescriptorPoolSize descriptorPoolSize = new()
        {
            type = VkDescriptorType.UniformBuffer,
            descriptorCount = 1
        };

        VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new()
        {
            sType = VkStructureType.DescriptorPoolCreateInfo,
            maxSets = 1,
            poolSizeCount = 1,
            pPoolSizes = &descriptorPoolSize
        };

        if (vkCreateDescriptorPool(logicalDevice.Device, &descriptorPoolCreateInfo, null, out descriptorPool) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create descriptor pool!");
        }

        //var layouts = Enumerable.Range(0, (int)graphicsConfiguration.FramesInFlight).Select(x => descriptorSetLayout).ToArray();
        VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
        descriptorSetAllocateInfo.descriptorPool = descriptorPool;
        descriptorSetAllocateInfo.descriptorSetCount = 1u;
        fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &descriptorSetLayout)
        {
            descriptorSetAllocateInfo.pSetLayouts = pDescriptorSetLayout;
        }

        if (vkAllocateDescriptorSets(logicalDevice.Device, &descriptorSetAllocateInfo, out descriptorSet) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate descriptor set!");
        }

        for (int i = 0; i < graphicsConfiguration.FramesInFlight; i++)
        {
            var descriptorBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = vulkanBuffer.buffer,
                offset = 0,
                range = (ulong)Unsafe.SizeOf<T>()
            };

            VkWriteDescriptorSet writeDescriptorSet = VkWriteDescriptorSet.New();
            writeDescriptorSet.dstSet = descriptorSet;
            writeDescriptorSet.dstBinding = 0;
            writeDescriptorSet.descriptorCount = 1;
            writeDescriptorSet.descriptorType = VkDescriptorType.UniformBuffer;
            writeDescriptorSet.pBufferInfo = &descriptorBufferInfo;

            vkUpdateDescriptorSets(logicalDevice.Device, 1, &writeDescriptorSet, 0, null);
        }
    }

    public void Cleanup()
    {
        vkDestroyDescriptorPool(logicalDevice.Device, descriptorPool, IntPtr.Zero);
        vkDestroyDescriptorSetLayout(logicalDevice.Device, descriptorSetLayout, IntPtr.Zero);
    }
}
