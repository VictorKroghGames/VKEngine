using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanDescriptorSetFactory(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSetFactory
{
    public IDescriptorSet CreateDescriptorSet<T>(IBuffer buffer, ITexture texture)
    {
        return CreateDescriptorSet<T>(graphicsConfiguration.FramesInFlight, buffer, texture);
    }

    public IDescriptorSet CreateDescriptorSet<T>(uint maxSets, IBuffer buffer, ITexture texture)
    {
        if (buffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
        }

        if (texture is not VulkanTexture vulkanTexture)
        {
            throw new InvalidOperationException("Invalid texture type!");
        }

        var descriptorSet = new VulkanDescriptorSet(graphicsConfiguration, logicalDevice);
        descriptorSet.Initialize<T>(maxSets, vulkanBuffer, vulkanTexture);
        return descriptorSet;
    }
}

internal sealed class VulkanDescriptorSet(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IDescriptorSet
{
    private VkDescriptorPool descriptorPool = VkDescriptorPool.Null;
    internal VkDescriptorSetLayout descriptorSetLayout = VkDescriptorSetLayout.Null;
    internal VkDescriptorSet[] descriptorSets = [];

    internal unsafe void Initialize<T>(uint maxSets, VulkanBuffer vulkanBuffer, VulkanTexture vulkanTexture)
    {
        CreateDescriptorPool(maxSets);

        var descriptorSetLayoutBindings = new VkDescriptorSetLayoutBinding[] {
            new() {
                binding = 0,
                descriptorType = VkDescriptorType.UniformBuffer,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Vertex,
                pImmutableSamplers = null
            },
            new() {
                binding = 1,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Fragment,
                pImmutableSamplers = null
            }
        };

        //var descriptorSetLayoutBinding = new VkDescriptorSetLayoutBinding
        //{
        //    binding = 0,
        //    descriptorType = VkDescriptorType.UniformBuffer,
        //    descriptorCount = 1,
        //    stageFlags = VkShaderStageFlags.Vertex,
        //    pImmutableSamplers = null
        //};

        var descriptorSetLayoutCreateInfo = VkDescriptorSetLayoutCreateInfo.New();
        fixed (VkDescriptorSetLayoutBinding* pDescriptorSetLayoutBindings = &descriptorSetLayoutBindings[0])
        {
            descriptorSetLayoutCreateInfo.bindingCount = (uint)descriptorSetLayoutBindings.Length;
            descriptorSetLayoutCreateInfo.pBindings = pDescriptorSetLayoutBindings;
        }
        //descriptorSetLayoutCreateInfo.bindingCount = 1;
        //descriptorSetLayoutCreateInfo.pBindings = &descriptorSetLayoutBinding;

        if (vkCreateDescriptorSetLayout(logicalDevice.Device, &descriptorSetLayoutCreateInfo, null, out descriptorSetLayout) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create descriptor set layout!");
        }

        var layouts = Enumerable.Range(0, (int)graphicsConfiguration.FramesInFlight).Select(x => descriptorSetLayout).ToArray();
        VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
        descriptorSetAllocateInfo.descriptorPool = descriptorPool;
        descriptorSetAllocateInfo.descriptorSetCount = (uint)layouts.Length;
        fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &layouts[0])
        {
            descriptorSetAllocateInfo.pSetLayouts = pDescriptorSetLayout;
        }

        descriptorSets = new VkDescriptorSet[graphicsConfiguration.FramesInFlight];
        fixed (VkDescriptorSet* pDescriptorSets = &descriptorSets[0])
        {
            if (vkAllocateDescriptorSets(logicalDevice.Device, &descriptorSetAllocateInfo, pDescriptorSets) is not VkResult.Success)
            {
                throw new InvalidOperationException("Failed to allocate descriptor set!");
            }
        }

        for (int i = 0; i < graphicsConfiguration.FramesInFlight; i++)
        {
            var descriptorBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = vulkanBuffer.buffer,
                offset = 0,
                range = (ulong)Unsafe.SizeOf<T>()
            };

            var descriptorImageInfo = vulkanTexture.GetDescriptorImageInfo();

            var writeDescriptorSets = new VkWriteDescriptorSet[]
            {
                new()
                {
                    sType = VkStructureType.WriteDescriptorSet,
                    dstSet = descriptorSets[i],
                    dstBinding = 0,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.UniformBuffer,
                    descriptorCount = 1,
                    pBufferInfo = &descriptorBufferInfo
                },
                new()
                {
                    sType = VkStructureType.WriteDescriptorSet,
                    dstSet = descriptorSets[i],
                    dstBinding = 1,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    descriptorCount = 1,
                    pImageInfo = &descriptorImageInfo
                }
            };

            //VkWriteDescriptorSet writeDescriptorSet = VkWriteDescriptorSet.New();
            //writeDescriptorSet.dstSet = descriptorSets[i];
            //writeDescriptorSet.dstBinding = 0;
            //writeDescriptorSet.dstArrayElement = 0;
            //writeDescriptorSet.descriptorType = VkDescriptorType.UniformBuffer;
            //writeDescriptorSet.descriptorCount = 1;
            //writeDescriptorSet.pBufferInfo = &descriptorBufferInfo;

            fixed(VkWriteDescriptorSet* writeDescriptorSet = &writeDescriptorSets[0])
            {
                vkUpdateDescriptorSets(logicalDevice.Device, (uint)writeDescriptorSets.Length, writeDescriptorSet, 0, null);
            }
            //vkUpdateDescriptorSets(logicalDevice.Device, writeDescriptorSets.Length, &writeDescriptorSet[0], 0, null);
        }
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
