using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanRenderPass
{
    VkRenderPass Raw { get; }

    void Initialize();
}

internal sealed class VulkanRenderPass(IVulkanLogicalDevice vulkanLogicalDevice, IVulkanSwapChain vulkanSwapChain) : IVulkanRenderPass
{
    private VkRenderPass renderPass = VkRenderPass.Null;

    public VkRenderPass Raw => renderPass;

    public unsafe void Initialize()
    {
        var colorAttachment = new VkAttachmentDescription();
        colorAttachment.format = vulkanSwapChain.SurfaceFormat.format;
        colorAttachment.samples = VkSampleCountFlags.Count1;
        colorAttachment.loadOp = VkAttachmentLoadOp.Clear;
        colorAttachment.storeOp = VkAttachmentStoreOp.Store;
        colorAttachment.stencilLoadOp = VkAttachmentLoadOp.DontCare;
        colorAttachment.stencilStoreOp = VkAttachmentStoreOp.DontCare;
        colorAttachment.initialLayout = VkImageLayout.Undefined;
        colorAttachment.finalLayout = VkImageLayout.PresentSrcKHR;

        var colorAttachmentRef = new VkAttachmentReference();
        colorAttachmentRef.attachment = 0;
        colorAttachmentRef.layout = VkImageLayout.ColorAttachmentOptimal;

        var subpass = new VkSubpassDescription();
        subpass.pipelineBindPoint = VkPipelineBindPoint.Graphics;
        subpass.colorAttachmentCount = 1;
        subpass.pColorAttachments = &colorAttachmentRef;

        var renderPassCreateInfo = VkRenderPassCreateInfo.New();
        renderPassCreateInfo.attachmentCount = 1;
        renderPassCreateInfo.pAttachments = &colorAttachment;
        renderPassCreateInfo.subpassCount = 1;
        renderPassCreateInfo.pSubpasses = &subpass;

        var dependency = new VkSubpassDependency();
        dependency.srcSubpass = SubpassExternal;
        dependency.dstSubpass = 0;
        dependency.srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        dependency.srcAccessMask = 0;
        dependency.dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        dependency.dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite;

        renderPassCreateInfo.dependencyCount = 1;
        renderPassCreateInfo.pDependencies = &dependency;

        var result = vkCreateRenderPass(vulkanLogicalDevice.Device, ref renderPassCreateInfo, null, out renderPass);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create render pass");
        }
    }
}
