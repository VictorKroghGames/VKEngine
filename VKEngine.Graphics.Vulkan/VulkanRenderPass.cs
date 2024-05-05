using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanRenderPassFactory(IVulkanLogicalDevice logicalDevice) : IRenderPassFactory
{
    public IRenderPass CreateRenderPass()
    {
        var renderPass = new VulkanRenderPass(logicalDevice);
        renderPass.Initialize();
        return renderPass;
    }
}

internal sealed class VulkanRenderPass(IVulkanLogicalDevice logicalDevice) : IRenderPass
{
    internal VkRenderPass renderPass;

    internal unsafe void Initialize()
    {
        var attachmentDescription = new VkAttachmentDescription
        {
            format = VkFormat.B8g8r8a8Unorm,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };

        var colorAttachmentReference = new VkAttachmentReference
        {
            attachment = 0,
            layout = VkImageLayout.ColorAttachmentOptimal
        };

        var subpassDescription = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentReference
        };

        var renderPassCreateInfo = VkRenderPassCreateInfo.New();
        renderPassCreateInfo.attachmentCount = 1;
        renderPassCreateInfo.pAttachments = &attachmentDescription;
        renderPassCreateInfo.subpassCount = 1;
        renderPassCreateInfo.pSubpasses = &subpassDescription;

        vkCreateRenderPass(logicalDevice.Device, &renderPassCreateInfo, null, out renderPass);
    }

    public void Cleanup()
    {
        vkDestroyRenderPass(logicalDevice.Device, renderPass, IntPtr.Zero);
    }

    public void Bind()
    {
    }

    public void Unbind()
    {
    }
}
