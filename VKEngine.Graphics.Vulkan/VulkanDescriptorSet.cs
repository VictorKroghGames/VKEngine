using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanDescriptorSetFactory(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSetFactory
{
    public IDescriptorSet CreateDescriptorSet(DescriptorSetDescription descriptorSetDescription)
    {
        return CreateDescriptorSet(1, descriptorSetDescription);
    }

    public IDescriptorSet CreateDescriptorSet(uint maxSets, DescriptorSetDescription descriptorSetDescription)
    {
        var descriptorSet = new VulkanDescriptorSet(graphicsConfiguration, logicalDevice);
        descriptorSet.Initialize(maxSets, descriptorSetDescription);
        return descriptorSet;
    }
}

internal sealed class VulkanDescriptorSet(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSet
{
    private VkDescriptorPool descriptorPool = VkDescriptorPool.Null;
    internal VkDescriptorSetLayout descriptorSetLayout = VkDescriptorSetLayout.Null;
    internal VkDescriptorSet descriptorSet = VkDescriptorSet.Null;

    internal unsafe void Initialize(uint maxSets, DescriptorSetDescription descriptorSetDescription)
    {
        CreateDescriptorPool(maxSets);

        var descriptorSetLayoutBindings = descriptorSetDescription.DescriptorBindings.Select(x => new VkDescriptorSetLayoutBinding
        {
            binding = x.Binding,
            descriptorType = x.DescriptorType switch
            {
                DescriptorType.UniformBuffer => VkDescriptorType.UniformBuffer,
                DescriptorType.CombinedImageSampler => VkDescriptorType.CombinedImageSampler,
                _ => throw new InvalidOperationException("Invalid descriptor type!")
            },
            descriptorCount = x.DescriptorCount,
            stageFlags = x.ShaderStageFlags switch
            {
                ShaderModuleType.Vertex => VkShaderStageFlags.Vertex,
                ShaderModuleType.Fragment => VkShaderStageFlags.Fragment,
                _ => throw new InvalidOperationException("Invalid shader stage flags!")
            },
            pImmutableSamplers = null
        }).ToArray();

        var descriptorSetLayoutCreateInfo = VkDescriptorSetLayoutCreateInfo.New();
        fixed (VkDescriptorSetLayoutBinding* pDescriptorSetLayoutBindings = &descriptorSetLayoutBindings[0])
        {
            descriptorSetLayoutCreateInfo.bindingCount = (uint)descriptorSetLayoutBindings.Length;
            descriptorSetLayoutCreateInfo.pBindings = pDescriptorSetLayoutBindings;
        }

        if (vkCreateDescriptorSetLayout(logicalDevice.Device, &descriptorSetLayoutCreateInfo, null, out descriptorSetLayout) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create descriptor set layout!");
        }

        AllocateDescriptorSet(descriptorSetLayout);
    }

    private unsafe void AllocateDescriptorSet(params VkDescriptorSetLayout[] layouts)
    {
        VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
        descriptorSetAllocateInfo.descriptorPool = descriptorPool;
        descriptorSetAllocateInfo.descriptorSetCount = (uint)layouts.Length;
        fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &layouts[0])
        {
            descriptorSetAllocateInfo.pSetLayouts = pDescriptorSetLayout;
        }

        fixed (VkDescriptorSet* pDescriptorSets = &descriptorSet)
        {
            if (vkAllocateDescriptorSets(logicalDevice.Device, &descriptorSetAllocateInfo, pDescriptorSets) is not VkResult.Success)
            {
                throw new InvalidOperationException("Failed to allocate descriptor set!");
            }
        }
    }

    public void Update<T>(uint binding, uint arrayElement, IBuffer buffer, ulong offset = 0)
    {
        var size = (ulong)Unsafe.SizeOf<T>();
        Update(binding, arrayElement, buffer, size, offset);
    }

    public unsafe void Update(uint binding, uint arrayElement, IBuffer buffer, ulong size, ulong offset = 0)
    {
        if (buffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
        }

        var descriptorBufferInfo = new VkDescriptorBufferInfo
        {
            buffer = vulkanBuffer.buffer,
            offset = offset,
            range = size
        };

        var writeDescriptorSet = VkWriteDescriptorSet.New();
        writeDescriptorSet.dstSet = descriptorSet;
        writeDescriptorSet.dstBinding = binding;
        writeDescriptorSet.descriptorType = VkDescriptorType.UniformBuffer;
        writeDescriptorSet.dstArrayElement = arrayElement;
        writeDescriptorSet.descriptorCount = 1;
        writeDescriptorSet.pBufferInfo = &descriptorBufferInfo;

        vkUpdateDescriptorSets(logicalDevice.Device, 1, &writeDescriptorSet, 0, null);
    }

    public unsafe void Update(uint binding, uint arrayElement, ITexture texture)
    {
        if(texture is not VulkanTexture vulkanTexture)
        {
            throw new InvalidOperationException("Invalid texture type!");
        }

        var descriptorImageInfo = vulkanTexture.GetDescriptorImageInfo();

        var writeDescriptorSet = VkWriteDescriptorSet.New();
        writeDescriptorSet.dstSet = descriptorSet;
        writeDescriptorSet.dstBinding = binding;
        writeDescriptorSet.descriptorType = VkDescriptorType.CombinedImageSampler;
        writeDescriptorSet.dstArrayElement = arrayElement;
        writeDescriptorSet.descriptorCount = 1;
        writeDescriptorSet.pImageInfo = &descriptorImageInfo;

        vkUpdateDescriptorSets(logicalDevice.Device, 1, &writeDescriptorSet, 0, null);
    }

    public void Cleanup()
    {
        vkDestroyDescriptorPool(logicalDevice.Device, descriptorPool, IntPtr.Zero);
        vkDestroyDescriptorSetLayout(logicalDevice.Device, descriptorSetLayout, IntPtr.Zero);
    }

    private unsafe void CreateDescriptorPool(uint maxSets)
    {
        var descriptorPoolSize = new VkDescriptorPoolSize[]
        {
            new() {
                type = VkDescriptorType.UniformBuffer,
                descriptorCount = graphicsConfiguration.FramesInFlight
            },
            new() {
                type = VkDescriptorType.CombinedImageSampler,
                descriptorCount = graphicsConfiguration.FramesInFlight
            }
        };

        //VkDescriptorPoolSize descriptorPoolSize = new()
        //{
        //    type = VkDescriptorType.UniformBuffer,
        //    descriptorCount = 1
        //};

        var descriptorPoolCreateInfo = VkDescriptorPoolCreateInfo.New();
        descriptorPoolCreateInfo.maxSets = maxSets;
        fixed (VkDescriptorPoolSize* pDescriptorPoolSize = &descriptorPoolSize[0])
        {
            descriptorPoolCreateInfo.poolSizeCount = (uint)descriptorPoolSize.Length;
            descriptorPoolCreateInfo.pPoolSizes = pDescriptorPoolSize;
        }

        if (vkCreateDescriptorPool(logicalDevice.Device, &descriptorPoolCreateInfo, null, out descriptorPool) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create descriptor pool!");
        }
    }
}
