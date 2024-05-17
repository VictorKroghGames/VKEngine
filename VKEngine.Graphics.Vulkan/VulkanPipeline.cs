using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using VKEngine.Graphics.Vulkan.Helpers;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanPipelineFactory(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : IPipelineFactory
{
    public IPipeline CreateGraphicsPipeline(PipelineDescription pipelineDescription)
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Invalid swap chain type!");
        }

        if (pipelineDescription.Shader is not VulkanShader vulkanShader)
        {
            throw new InvalidOperationException("Invalid shader type!");
        }

        var vulkanPipeline = new VulkanPipeline(graphicsConfiguration, logicalDevice);
        vulkanPipeline.Initialize(pipelineDescription, vulkanSwapChain, vulkanShader);
        return vulkanPipeline;
    }
}

internal sealed class VulkanPipeline(IGraphicsConfiguration graphicsConfiguration, IVulkanLogicalDevice logicalDevice) : IPipeline
{
    internal VkPipeline pipeline;
    internal VkPipelineLayout pipelineLayout;

    internal unsafe void Initialize(PipelineDescription description, VulkanSwapChain swapChain, VulkanShader shader)
    {
        // SHADERS
        var shaderModules = shader.GetShaderModules().ToArray();

        var pipelineShaderStageCreateInfo = stackalloc VkPipelineShaderStageCreateInfo[shaderModules.Length];
        for (int i = 0; i < shaderModules.Length; i++)
        {
            pipelineShaderStageCreateInfo[i] = VkPipelineShaderStageCreateInfo.New();
            pipelineShaderStageCreateInfo[i].stage = shaderModules[i].ShaderStageFlags;
            pipelineShaderStageCreateInfo[i].module = shaderModules[i].Module;
            pipelineShaderStageCreateInfo[i].pName = Strings.main;
        }

        // VERTEX INPUT
        var vertexInputBindingAndAttributes = VkVertexInputHelper.GetVertexInputBindingAndAttributeDescriptions(description.VertexLayouts).ToArray();

        var vertexInputBindings = vertexInputBindingAndAttributes.Select(x => x.VertexInputBindingDescription).ToArray();
        var vertexInputAttributes = vertexInputBindingAndAttributes.SelectMany(x => x.VertexInputAttributeDescriptions).ToArray();

        var pipelineVertexInputStateCreateInfo = VkPipelineVertexInputStateCreateInfo.New();
        fixed (VkVertexInputBindingDescription* pVertexInputBindingDescriptions = &vertexInputBindings[0])
        {
            pipelineVertexInputStateCreateInfo.vertexBindingDescriptionCount = (uint)vertexInputBindings.Length;
            pipelineVertexInputStateCreateInfo.pVertexBindingDescriptions = pVertexInputBindingDescriptions;
        }
        fixed (VkVertexInputAttributeDescription* pVertexInputAttributeDescriptions = &vertexInputAttributes[0])
        {
            pipelineVertexInputStateCreateInfo.vertexAttributeDescriptionCount = (uint)vertexInputAttributes.Length;
            pipelineVertexInputStateCreateInfo.pVertexAttributeDescriptions = pVertexInputAttributeDescriptions;
        }

        // INPUT ASSEMBLY
        var pipelineInputAssemblyStateCreateInfo = VkPipelineInputAssemblyStateCreateInfo.New();
        pipelineInputAssemblyStateCreateInfo.topology = (VkPrimitiveTopology)description.PrimitiveTopology;
        pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = description.PrimitiveRestartEnable;

        // VIEWPORT AND SCISSOR
        var extent = new VkExtent2D
        {
            width = swapChain.extent.width,
            height = swapChain.extent.height
        };

        var viewport = new VkViewport
        {
            x = 0.0f,
            y = 0.0f,
            width = extent.width,
            height = extent.height,
            minDepth = 0.0f,
            maxDepth = 1.0f
        };

        var scissor = new VkRect2D
        {
            offset = VkOffset2D.Zero,
            extent = extent
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
        pipelineRasterizationStateCreateInfo.cullMode = (VkCullModeFlags)description.CullMode;
        pipelineRasterizationStateCreateInfo.frontFace = (VkFrontFace)description.FrontFace;
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

        // DYNAMIC STATE
        const int dynamicStateCount = 2;
        var dynamicStates = stackalloc VkDynamicState[dynamicStateCount]
        {
            VkDynamicState.Viewport,
            VkDynamicState.Scissor
        };

        var pipelineDynamicStateCreateInfo = VkPipelineDynamicStateCreateInfo.New();
        pipelineDynamicStateCreateInfo.dynamicStateCount = (uint)dynamicStateCount;
        pipelineDynamicStateCreateInfo.pDynamicStates = &dynamicStates[0];

        // PIPELINE LAYOUT
        CreatePipelineLayout(description.DescriptorSets);

        var graphicsPipelineCreateInfo = VkGraphicsPipelineCreateInfo.New();
        graphicsPipelineCreateInfo.stageCount = (uint)shaderModules.Length;
        graphicsPipelineCreateInfo.pStages = pipelineShaderStageCreateInfo;
        graphicsPipelineCreateInfo.pVertexInputState = &pipelineVertexInputStateCreateInfo;
        graphicsPipelineCreateInfo.pInputAssemblyState = &pipelineInputAssemblyStateCreateInfo;
        graphicsPipelineCreateInfo.pViewportState = &pipelineViewportStateCreateInfo;
        graphicsPipelineCreateInfo.pRasterizationState = &pipelineRasterizationStateCreateInfo;
        graphicsPipelineCreateInfo.pMultisampleState = &pipelineMultisampleStateCreateInfo;
        graphicsPipelineCreateInfo.pDepthStencilState = null;
        graphicsPipelineCreateInfo.pColorBlendState = &pipelineColorBlendStateCreateInfo;
        graphicsPipelineCreateInfo.pDynamicState = &pipelineDynamicStateCreateInfo;
        graphicsPipelineCreateInfo.layout = pipelineLayout;

        graphicsPipelineCreateInfo.renderPass = (swapChain.RenderPass as VulkanRenderPass)!.renderPass;
        graphicsPipelineCreateInfo.subpass = 0;
        graphicsPipelineCreateInfo.basePipelineHandle = VkPipeline.Null;
        graphicsPipelineCreateInfo.basePipelineIndex = -1;

        if (vkCreateGraphicsPipelines(logicalDevice.Device, VkPipelineCache.Null, 1u, &graphicsPipelineCreateInfo, null, out pipeline) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create graphics pipeline!");
        }
    }

    private unsafe void CreatePipelineLayout(IEnumerable<IDescriptorSet> descriptorSets)
    {
        {
            var descriptorSetLayouts = descriptorSets.OfType<VulkanDescriptorSet>().Select(x => x.descriptorSetLayout).ToArray();
            var pushConstantRanges = stackalloc VkPushConstantRange[0];

            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            fixed (VkDescriptorSetLayout* pDescriptorSetLayouts = &descriptorSetLayouts[0])
            {
                pipelineLayoutCreateInfo.setLayoutCount = (uint)descriptorSetLayouts.Length;
                pipelineLayoutCreateInfo.pSetLayouts = pDescriptorSetLayouts;
            }
            pipelineLayoutCreateInfo.pushConstantRangeCount = 0;
            pipelineLayoutCreateInfo.pPushConstantRanges = pushConstantRanges;

            if (vkCreatePipelineLayout(logicalDevice.Device, &pipelineLayoutCreateInfo, null, out pipelineLayout) is not VkResult.Success)
            {
                throw new InvalidOperationException("Failed to create pipeline layout!");
            }
        }
    }

    public void Cleanup()
    {
        vkDestroyPipeline(logicalDevice.Device, pipeline, IntPtr.Zero);
        vkDestroyPipelineLayout(logicalDevice.Device, pipelineLayout, IntPtr.Zero);
    }

    public void Bind()
    {
    }

    public void Unbind()
    {
    }
}
