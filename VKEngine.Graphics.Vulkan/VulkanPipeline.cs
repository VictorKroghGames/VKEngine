using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanPipeline
{
    VkPipeline Raw { get; }

    void Initialize(IShader shader, VkRenderPass vkRenderPass, VkPipelineLayout pipelineLayout);
}

internal sealed class VulkanPipeline(IWindow window, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanPipeline
{
    private VkPipeline graphicsPipeline;

    public VkPipeline Raw => graphicsPipeline;

    public unsafe void Initialize(IShader shader, VkRenderPass renderPass, VkPipelineLayout pipelineLayout)
    {
        if (shader is not IVulkanShader vulkanShader)
        {
            throw new Exception("Shader must be of type IVulkanShader");
        }

        var shaderModules = vulkanShader.GetShaderModules();

        var pipelineShaderStaeCreateInfos = shaderModules.Select(module =>
        {
            var pipelineShaderStaeCreateInfo = VkPipelineShaderStageCreateInfo.New();
            pipelineShaderStaeCreateInfo.stage = module.ShaderStageFlags;
            pipelineShaderStaeCreateInfo.module = module.Module;
            pipelineShaderStaeCreateInfo.pName = module.MainFunctionIdentifier;
            return pipelineShaderStaeCreateInfo;
        }).ToArray();

        var pipelineVertexInputStateCreateInfo = VkPipelineVertexInputStateCreateInfo.New();
        var vertexBindingDesc = Vertex.GetBindingDescription();
        var attributeDescr = Vertex.GetAttributeDescriptions();
        pipelineVertexInputStateCreateInfo.vertexBindingDescriptionCount = 1;
        pipelineVertexInputStateCreateInfo.pVertexBindingDescriptions = &vertexBindingDesc;
        pipelineVertexInputStateCreateInfo.vertexAttributeDescriptionCount = attributeDescr.Count;
        pipelineVertexInputStateCreateInfo.pVertexAttributeDescriptions = &attributeDescr.First;

        var pipelineInputAssemblyStateCreateInfo = VkPipelineInputAssemblyStateCreateInfo.New();
        pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = false;
        pipelineInputAssemblyStateCreateInfo.topology = VkPrimitiveTopology.TriangleList;

        var viewport = GetViewport();

        var extent = new VkExtent2D(window.Width, window.Height);

        VkRect2D scissorRect = new(extent);

        var pipelineViewportStateCreateInfo = VkPipelineViewportStateCreateInfo.New();
        pipelineViewportStateCreateInfo.viewportCount = 1;
        pipelineViewportStateCreateInfo.pViewports = &viewport;
        pipelineViewportStateCreateInfo.scissorCount = 1;
        pipelineViewportStateCreateInfo.pScissors = &scissorRect;

        var pipelineRasterizationStateCreateInfo = VkPipelineRasterizationStateCreateInfo.New();
        pipelineRasterizationStateCreateInfo.cullMode = VkCullModeFlags.Back;
        pipelineRasterizationStateCreateInfo.polygonMode = VkPolygonMode.Fill;
        pipelineRasterizationStateCreateInfo.lineWidth = 1f;
        pipelineRasterizationStateCreateInfo.frontFace = VkFrontFace.CounterClockwise;

        var pipelineMultisampleStateCreateInfo = VkPipelineMultisampleStateCreateInfo.New();
        pipelineMultisampleStateCreateInfo.rasterizationSamples = VkSampleCountFlags.Count1;
        pipelineMultisampleStateCreateInfo.minSampleShading = 1f;

        var colorBlendAttachementState = new VkPipelineColorBlendAttachmentState();
        colorBlendAttachementState.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
        colorBlendAttachementState.blendEnable = false;

        var pipelineColorBlendStateCreateInfo = VkPipelineColorBlendStateCreateInfo.New();
        pipelineColorBlendStateCreateInfo.attachmentCount = 1;
        pipelineColorBlendStateCreateInfo.pAttachments = &colorBlendAttachementState;

        fixed (VkPipelineShaderStageCreateInfo* pipelineShaderStageCreateInfoPtr = &pipelineShaderStaeCreateInfos.ToArray()[0])
        {
            VkGraphicsPipelineCreateInfo graphicsPipelineCreateInfo = VkGraphicsPipelineCreateInfo.New();
            graphicsPipelineCreateInfo.stageCount = (uint)pipelineShaderStaeCreateInfos.Count();
            graphicsPipelineCreateInfo.pStages = pipelineShaderStageCreateInfoPtr;

            graphicsPipelineCreateInfo.pVertexInputState = &pipelineVertexInputStateCreateInfo;
            graphicsPipelineCreateInfo.pInputAssemblyState = &pipelineInputAssemblyStateCreateInfo;
            graphicsPipelineCreateInfo.pViewportState = &pipelineViewportStateCreateInfo;
            graphicsPipelineCreateInfo.pRasterizationState = &pipelineRasterizationStateCreateInfo;
            graphicsPipelineCreateInfo.pMultisampleState = &pipelineMultisampleStateCreateInfo;
            graphicsPipelineCreateInfo.pColorBlendState = &pipelineColorBlendStateCreateInfo;
            graphicsPipelineCreateInfo.layout = pipelineLayout;

            graphicsPipelineCreateInfo.renderPass = renderPass;
            graphicsPipelineCreateInfo.subpass = 0;

            var result = vkCreateGraphicsPipelines(vulkanLogicalDevice.Device, VkPipelineCache.Null, 1, ref graphicsPipelineCreateInfo, null, out graphicsPipeline);
            if (result != VkResult.Success)
            {
                throw new ApplicationException("Failed to create graphics pipeline");
            }
        }
    }

    private unsafe VkPipelineColorBlendStateCreateInfo GetVkPipelineColorBlendStateCreateInfoUnsafe()
    {
        var colorBlendAttachementState = GetVkPipelineColorBlendAttachmentState();

        VkPipelineColorBlendStateCreateInfo colorBlendStateCI = VkPipelineColorBlendStateCreateInfo.New();
        colorBlendStateCI.attachmentCount = 1;
        colorBlendStateCI.pAttachments = &colorBlendAttachementState;
        return colorBlendStateCI;
    }

    private VkPipelineColorBlendAttachmentState GetVkPipelineColorBlendAttachmentState()
    {
        var colorBlendAttachementState = new VkPipelineColorBlendAttachmentState();
        colorBlendAttachementState.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
        colorBlendAttachementState.blendEnable = false;
        return colorBlendAttachementState;
    }

    private VkPipelineMultisampleStateCreateInfo GetVkPipelineMultisampleStateCreateInfo()
    {
        VkPipelineMultisampleStateCreateInfo multisampleStateCI = VkPipelineMultisampleStateCreateInfo.New();
        multisampleStateCI.rasterizationSamples = VkSampleCountFlags.Count1;
        multisampleStateCI.minSampleShading = 1f;
        return multisampleStateCI;
    }

    private VkPipelineRasterizationStateCreateInfo GetPipelineRasterizationStateCreateInfo()
    {
        var rasterizerStateCI = VkPipelineRasterizationStateCreateInfo.New();
        rasterizerStateCI.cullMode = VkCullModeFlags.Back;
        rasterizerStateCI.polygonMode = VkPolygonMode.Fill;
        rasterizerStateCI.lineWidth = 1f;
        rasterizerStateCI.frontFace = VkFrontFace.CounterClockwise;
        return rasterizerStateCI;
    }

    private unsafe VkPipelineViewportStateCreateInfo GetPipelineViewportStateCreateInfoUnsafe()
    {
        var viewport = GetViewport();

        var extent = new VkExtent2D(window.Width, window.Height);

        VkRect2D scissorRect = new(extent);

        var viewportStateCI = VkPipelineViewportStateCreateInfo.New();
        viewportStateCI.viewportCount = 1;
        viewportStateCI.pViewports = &viewport;
        viewportStateCI.scissorCount = 1;
        viewportStateCI.pScissors = &scissorRect;
        return viewportStateCI;
    }

    private VkViewport GetViewport()
    {
        VkViewport viewport;
        viewport.x = 0;
        viewport.y = 0;
        viewport.width = window.Width;
        viewport.height = window.Height;
        viewport.minDepth = 0f;
        viewport.maxDepth = 1f;
        return viewport;
    }

    private VkPipelineInputAssemblyStateCreateInfo GetPipelineInputAssemblyStateCreateInfo()
    {
        var inputAssemblyCI = VkPipelineInputAssemblyStateCreateInfo.New();
        inputAssemblyCI.primitiveRestartEnable = false;
        inputAssemblyCI.topology = VkPrimitiveTopology.TriangleList;
        return inputAssemblyCI;
    }

    private unsafe VkPipelineVertexInputStateCreateInfo GetPipelineVertexInputStateCreateInfoUnsafe()
    {
        var vertexInputStateCI = VkPipelineVertexInputStateCreateInfo.New();
        var vertexBindingDesc = Vertex.GetBindingDescription();
        var attributeDescr = Vertex.GetAttributeDescriptions();
        vertexInputStateCI.vertexBindingDescriptionCount = 1;
        vertexInputStateCI.pVertexBindingDescriptions = &vertexBindingDesc;
        vertexInputStateCI.vertexAttributeDescriptionCount = attributeDescr.Count;
        vertexInputStateCI.pVertexAttributeDescriptions = &attributeDescr.First;
        return vertexInputStateCI;
    }

    private unsafe IEnumerable<VkPipelineShaderStageCreateInfo> GetPipelineShaderStageCreateInfoUnsafe(IShader shader)
    {
        if (shader is not IVulkanShader vulkanShader)
        {
            return Enumerable.Empty<VkPipelineShaderStageCreateInfo>();
        }

        var shaderModules = vulkanShader.GetShaderModules();

        return shaderModules.Select(module =>
        {
            var pipelineShaderStaeCreateInfo = VkPipelineShaderStageCreateInfo.New();
            pipelineShaderStaeCreateInfo.stage = module.ShaderStageFlags;
            pipelineShaderStaeCreateInfo.module = module.Module;
            pipelineShaderStaeCreateInfo.pName = module.MainFunctionIdentifier;
            return pipelineShaderStaeCreateInfo;
        });
    }


}
