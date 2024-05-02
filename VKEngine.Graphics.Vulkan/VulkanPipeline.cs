using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanPipeline
{
    VkPipeline Raw { get; }

    void Initialize(IShader shader, VkPipelineLayout pipelineLayout);
}

internal sealed class VulkanPipeline(IWindow window, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanPipeline
{
    private VkPipeline graphicsPipeline;

    public VkPipeline Raw => graphicsPipeline;

    public void Initialize(IShader shader, VkPipelineLayout pipelineLayout)
    {
        var pipelineShaderStaeCreateInfos = GetPipelineShaderStageCreateInfoUnsafe(shader);
        var pipelineVertexInputStateCreateInfo = GetPipelineVertexInputStateCreateInfoUnsafe();
        var pipelineInputAssemblyStateCreateInfo = GetPipelineInputAssemblyStateCreateInfo();
        var pipelineViewportStateCreateInfo = GetPipelineViewportStateCreateInfoUnsafe();
        var pipelineRasterizationStateCreateInfo = GetPipelineRasterizationStateCreateInfo();
        var pipelineMultisampleStateCreateInfo = GetVkPipelineMultisampleStateCreateInfo();
        var pipelineColorBlendStateCreateInfo = GetVkPipelineColorBlendStateCreateInfoUnsafe();

        var result = CreateGraphicsPipeline(pipelineShaderStaeCreateInfos, pipelineVertexInputStateCreateInfo, pipelineInputAssemblyStateCreateInfo, pipelineViewportStateCreateInfo, pipelineRasterizationStateCreateInfo, pipelineMultisampleStateCreateInfo, pipelineColorBlendStateCreateInfo, pipelineLayout);
        if (result != VkResult.Success) 
        {
            throw new ApplicationException("Failed to create graphics pipeline");
        }
    }

    private unsafe VkResult CreateGraphicsPipeline(IEnumerable<VkPipelineShaderStageCreateInfo> pipelineShaderStageCreateInfos, VkPipelineVertexInputStateCreateInfo vertexInputStateCI, VkPipelineInputAssemblyStateCreateInfo inputAssemblyCI, VkPipelineViewportStateCreateInfo viewportStateCI, VkPipelineRasterizationStateCreateInfo rasterizerStateCI, VkPipelineMultisampleStateCreateInfo multisampleStateCI, VkPipelineColorBlendStateCreateInfo colorBlendStateCI, VkPipelineLayout pipelineLayout)
    {
        fixed (VkPipelineShaderStageCreateInfo* pipelineShaderStageCreateInfoPtr = &pipelineShaderStageCreateInfos.ToArray()[0])
        {
            VkGraphicsPipelineCreateInfo graphicsPipelineCI = VkGraphicsPipelineCreateInfo.New();
            graphicsPipelineCI.stageCount = (uint)pipelineShaderStageCreateInfos.Count();
            graphicsPipelineCI.pStages = pipelineShaderStageCreateInfoPtr;

            graphicsPipelineCI.pVertexInputState = &vertexInputStateCI;
            graphicsPipelineCI.pInputAssemblyState = &inputAssemblyCI;
            graphicsPipelineCI.pViewportState = &viewportStateCI;
            graphicsPipelineCI.pRasterizationState = &rasterizerStateCI;
            graphicsPipelineCI.pMultisampleState = &multisampleStateCI;
            graphicsPipelineCI.pColorBlendState = &colorBlendStateCI;
            graphicsPipelineCI.layout = pipelineLayout;

            //graphicsPipelineCI.renderPass = _renderPass;
            //graphicsPipelineCI.subpass = 0;

            return vkCreateGraphicsPipelines(vulkanLogicalDevice.Device, VkPipelineCache.Null, 1, ref graphicsPipelineCI, null, out graphicsPipeline);
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
