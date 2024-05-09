using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanPipelineFactory(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : IPipelineFactory
{
    public IPipeline CreateGraphicsPipeline(PipelineSpecification specification)
    {
        var vulkanPipeline = new VulkanPipeline(graphicsConfiguration, logicalDevice, swapChain, specification);
        vulkanPipeline.Initialize();
        return vulkanPipeline;
    }
}

internal sealed class VulkanPipeline(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain, PipelineSpecification specification) : IPipeline
{
    internal VkPipeline pipeline;

    private VkDescriptorSetLayout descriptorSetLayout;
    internal VkPipelineLayout pipelineLayout;

    internal unsafe void Initialize()
    {
        if (specification.Shader is not VulkanShader shader)
        {
            throw new InvalidOperationException("Invalid shader type!");
        }

        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        if (specification.RenderPass is not VulkanRenderPass renderPass)
        {
            throw new InvalidOperationException("Invalid render pass type!");
        }

        var shaderModules = shader.GetShaderModules().ToArray();

        var pipelineShaderStageCreateInfo = stackalloc VkPipelineShaderStageCreateInfo[shaderModules.Length];
        for (int i = 0; i < shaderModules.Length; i++)
        {
            pipelineShaderStageCreateInfo[i] = VkPipelineShaderStageCreateInfo.New();
            pipelineShaderStageCreateInfo[i].stage = shaderModules[i].ShaderStageFlags;
            pipelineShaderStageCreateInfo[i].module = shaderModules[i].Module;
            pipelineShaderStageCreateInfo[i].pName = Strings.main;
        }

        var vertexInputBindingDescription = new VkVertexInputBindingDescription
        {
            binding = specification.PipelineLayout.binding,
            stride = specification.PipelineLayout.stride,
            inputRate = (VkVertexInputRate)specification.PipelineLayout.inputRate
        };

        var vertexInputAttributeDescriptions = specification.PipelineLayout.vertexAttributes.Select(x => new VkVertexInputAttributeDescription
        {
            binding = x.binding,
            location = x.location,
            format = (VkFormat)x.format,
            offset = x.offset
        }).ToArray();

        //var vertexInputAttributeDescriptions = stackalloc VkVertexInputAttributeDescription[specification.PipelineLayout.vertexAttributes.Length];
        //for (int i = 0; i < specification.PipelineLayout.vertexAttributes.Length; i++)
        //{
        //    vertexInputAttributeDescriptions[i] = new VkVertexInputAttributeDescription
        //    {
        //        binding = specification.PipelineLayout.vertexAttributes[i].binding,
        //        location = specification.PipelineLayout.vertexAttributes[i].location,
        //        format = (VkFormat)specification.PipelineLayout.vertexAttributes[i].format,
        //        offset = specification.PipelineLayout.vertexAttributes[i].offset
        //    };
        //}

        // DYNAMIC STATE
        const int dynamicStateCount = 2;
        var dynamicStates = stackalloc VkDynamicState[dynamicStateCount]
        {
            VkDynamicState.Viewport,
            VkDynamicState.Scissor
        };

        var vkPipelineDynamicStateCreateInfo = VkPipelineDynamicStateCreateInfo.New();
        vkPipelineDynamicStateCreateInfo.dynamicStateCount = (uint)dynamicStateCount;
        vkPipelineDynamicStateCreateInfo.pDynamicStates = &dynamicStates[0];

        // VERTEX INPUT
        var pipelineVertexInputStateCreateInfo = VkPipelineVertexInputStateCreateInfo.New();
        pipelineVertexInputStateCreateInfo.vertexBindingDescriptionCount = 1;
        pipelineVertexInputStateCreateInfo.pVertexBindingDescriptions = &vertexInputBindingDescription;

        fixed (VkVertexInputAttributeDescription* pVertexInputAttributeDescriptions = &vertexInputAttributeDescriptions[0])
        {
            pipelineVertexInputStateCreateInfo.vertexAttributeDescriptionCount = (uint)specification.PipelineLayout.vertexAttributes.Length;
            pipelineVertexInputStateCreateInfo.pVertexAttributeDescriptions = pVertexInputAttributeDescriptions;
        }

        // INPUT ASSEMBLY
        var pipelineInputAssemblyStateCreateInfo = VkPipelineInputAssemblyStateCreateInfo.New();
        pipelineInputAssemblyStateCreateInfo.topology = VkPrimitiveTopology.TriangleList;
        pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = false;

        // VIEWPORT AND SCISSOR
        VkViewport viewport = new VkViewport
        {
            x = 0.0f,
            y = 0.0f,
            width = vulkanSwapChain.extent.width,
            height = vulkanSwapChain.extent.height,
            minDepth = 0.0f,
            maxDepth = 1.0f
        };

        VkRect2D scissor = new()
        {
            offset = VkOffset2D.Zero,
            extent = vulkanSwapChain.extent
        };

        var pipelineViewportStateCreateInfo = VkPipelineViewportStateCreateInfo.New();
        pipelineViewportStateCreateInfo.viewportCount = 1;
        pipelineViewportStateCreateInfo.pViewports = &viewport;
        pipelineViewportStateCreateInfo.scissorCount = 1;
        pipelineViewportStateCreateInfo.pScissors = &scissor;

        // RASTERIZATION
        var pipelineRasterizationStateCreateInfo = VkPipelineRasterizationStateCreateInfo.New();
        pipelineRasterizationStateCreateInfo.depthClampEnable = false;
        pipelineRasterizationStateCreateInfo.rasterizerDiscardEnable = false;
        pipelineRasterizationStateCreateInfo.polygonMode = VkPolygonMode.Fill;
        pipelineRasterizationStateCreateInfo.lineWidth = 1.0f;
        pipelineRasterizationStateCreateInfo.cullMode = (VkCullModeFlags)specification.CullMode;
        pipelineRasterizationStateCreateInfo.frontFace = (VkFrontFace)specification.FrontFace;
        pipelineRasterizationStateCreateInfo.depthBiasEnable = false;
        pipelineRasterizationStateCreateInfo.depthBiasConstantFactor = 0.0f;
        pipelineRasterizationStateCreateInfo.depthBiasClamp = 0.0f;
        pipelineRasterizationStateCreateInfo.depthBiasSlopeFactor = 0.0f;

        // MULTISAMPLING
        var pipelineMultisampleStateCreateInfo = VkPipelineMultisampleStateCreateInfo.New();
        pipelineMultisampleStateCreateInfo.sampleShadingEnable = false;
        pipelineMultisampleStateCreateInfo.rasterizationSamples = VkSampleCountFlags.Count1;
        pipelineMultisampleStateCreateInfo.minSampleShading = 1.0f;
        pipelineMultisampleStateCreateInfo.pSampleMask = null;
        pipelineMultisampleStateCreateInfo.alphaToCoverageEnable = false;
        pipelineMultisampleStateCreateInfo.alphaToOneEnable = false;

        // DEPTH AND STENCIL TESTING
        // VkPipelineDepthStencilStateCreateInfo pipelineDepthStencilStateCreateInfo = VkPipelineDepthStencilStateCreateInfo.New();

        // COLOR BLENDING
        var pipelineColorBlendAttachmentState = new VkPipelineColorBlendAttachmentState
        {
            colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
            blendEnable = false,
            srcColorBlendFactor = VkBlendFactor.One,
            dstColorBlendFactor = VkBlendFactor.Zero,
            colorBlendOp = VkBlendOp.Add,
            srcAlphaBlendFactor = VkBlendFactor.One,
            dstAlphaBlendFactor = VkBlendFactor.Zero,
            alphaBlendOp = VkBlendOp.Add
        };

        var pipelineColorBlendStateCreateInfo = VkPipelineColorBlendStateCreateInfo.New();
        pipelineColorBlendStateCreateInfo.logicOpEnable = false;
        pipelineColorBlendStateCreateInfo.logicOp = VkLogicOp.Copy;
        pipelineColorBlendStateCreateInfo.attachmentCount = 1;
        pipelineColorBlendStateCreateInfo.pAttachments = &pipelineColorBlendAttachmentState;
        pipelineColorBlendStateCreateInfo.blendConstants_0 = 0.0f;
        pipelineColorBlendStateCreateInfo.blendConstants_1 = 0.0f;
        pipelineColorBlendStateCreateInfo.blendConstants_2 = 0.0f;
        pipelineColorBlendStateCreateInfo.blendConstants_3 = 0.0f;

        // PIPELINE LAYOUT
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

            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            //pipelineLayoutCreateInfo.setLayoutCount = 0;
            //pipelineLayoutCreateInfo.pSetLayouts = null;
            pipelineLayoutCreateInfo.setLayoutCount = 1;
            fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &descriptorSetLayout)
            {
                pipelineLayoutCreateInfo.pSetLayouts = pDescriptorSetLayout;
            }
            pipelineLayoutCreateInfo.pushConstantRangeCount = 0;
            pipelineLayoutCreateInfo.pPushConstantRanges = null;

            if (vkCreatePipelineLayout(logicalDevice.Device, &pipelineLayoutCreateInfo, null, out pipelineLayout) is not VkResult.Success)
            {
                throw new InvalidOperationException("Failed to create pipeline layout!");
            }
        }

        var graphicsPipelineCreateInfo = VkGraphicsPipelineCreateInfo.New();
        graphicsPipelineCreateInfo.stageCount = (uint)shaderModules.Length;
        graphicsPipelineCreateInfo.pStages = &pipelineShaderStageCreateInfo[0];
        graphicsPipelineCreateInfo.pDynamicState = &vkPipelineDynamicStateCreateInfo;
        graphicsPipelineCreateInfo.pVertexInputState = &pipelineVertexInputStateCreateInfo;
        graphicsPipelineCreateInfo.pInputAssemblyState = &pipelineInputAssemblyStateCreateInfo;
        graphicsPipelineCreateInfo.pViewportState = &pipelineViewportStateCreateInfo;
        graphicsPipelineCreateInfo.pRasterizationState = &pipelineRasterizationStateCreateInfo;
        graphicsPipelineCreateInfo.pMultisampleState = &pipelineMultisampleStateCreateInfo;
        graphicsPipelineCreateInfo.pDepthStencilState = null;
        graphicsPipelineCreateInfo.pColorBlendState = &pipelineColorBlendStateCreateInfo;
        graphicsPipelineCreateInfo.layout = pipelineLayout;

        graphicsPipelineCreateInfo.renderPass = renderPass.renderPass;
        graphicsPipelineCreateInfo.subpass = 0;
        graphicsPipelineCreateInfo.basePipelineHandle = VkPipeline.Null;
        graphicsPipelineCreateInfo.basePipelineIndex = -1;

        if (vkCreateGraphicsPipelines(logicalDevice.Device, VkPipelineCache.Null, 1u, &graphicsPipelineCreateInfo, null, out pipeline) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create graphics pipeline!");
        }
    }

    public void Cleanup()
    {
        vkDestroyPipeline(logicalDevice.Device, pipeline, IntPtr.Zero);

        vkDestroyDescriptorPool(logicalDevice.Device, descriptorPool, IntPtr.Zero);

        vkDestroyDescriptorSetLayout(logicalDevice.Device, descriptorSetLayout, IntPtr.Zero);
        vkDestroyPipelineLayout(logicalDevice.Device, pipelineLayout, IntPtr.Zero);
    }

    private VkDescriptorPool descriptorPool = VkDescriptorPool.Null;
    internal VkDescriptorSet descriptorSet = VkDescriptorSet.Null;

    public unsafe void AddDescriptorSet<T>(IBuffer uniformBuffer)
    {
        if(uniformBuffer is not VulkanBuffer vulkanBuffer)
        {
            throw new InvalidOperationException("Invalid buffer type!");
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

        var layouts = Enumerable.Range(0, (int)graphicsConfiguration.FramesInFlight).Select(x => descriptorSetLayout).ToArray();
        VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
        descriptorSetAllocateInfo.descriptorPool = descriptorPool;
        descriptorSetAllocateInfo.descriptorSetCount = 1u;
        fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &descriptorSetLayout)
        {
            descriptorSetAllocateInfo.pSetLayouts = pDescriptorSetLayout;
        }

        if(vkAllocateDescriptorSets(logicalDevice.Device, &descriptorSetAllocateInfo, out descriptorSet) is not VkResult.Success)
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

    public void Bind()
    {
    }

    public void Unbind()
    {
    }
}
