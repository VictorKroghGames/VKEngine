using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanSwapChain : IDisposable
{
    VkSwapchainKHR Raw { get; }
    VkSurfaceFormatKHR SurfaceFormat { get; }
    RawList<VkImageView> ImageViews { get; }
    VkExtent2D Extent { get; }

    void Initialize(VkInstance vkInstance);
    void Present();
}

internal class VulkanSwapChain(IWindow window, IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanSwapChain
{
    private VkSwapchainKHR swapchain;
    private VkSurfaceKHR surface;
    private VkPresentModeKHR presentMode;
    private VkSurfaceFormatKHR surfaceFormat;
    private VkExtent2D extent = new VkExtent2D(window.Width, window.Height);

    private RawList<VkFence> waitFences;

    private RawList<VkImageView> imageViews = [];

    public VkSwapchainKHR Raw => swapchain;
    public VkSurfaceFormatKHR SurfaceFormat => surfaceFormat;
    public RawList<VkImageView> ImageViews => imageViews;
    public VkExtent2D Extent => extent;

    public void Initialize(VkInstance vkInstance)
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

        CreateSurfaceUnsafe(vkInstance);
        CreateSwapChainUnsafe();
        CreateImageViewsUnsafe();
    }

    public void Present()
    {
        var submitInfo = CreateSubmitInfoUnsafe();

        QueueSubmit(submitInfo);

        unsafe
        {
            //VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
            //presentInfo.waitSemaphoreCount = 1;
            //presentInfo.pWaitSemaphores = &signalSemaphore;

            //VkSwapchainKHR swapchain = vulkanSwapChain.Raw;
            //presentInfo.swapchainCount = 1;
            //presentInfo.pSwapchains = &swapchain;
            //presentInfo.pImageIndices = &imageIndex;

            //vkQueuePresentKHR(vulkanLogicalDevice.PresentQueue, ref presentInfo);
        }
    }

    private unsafe void QueueSubmit(VkSubmitInfo submitInfo)
    {
        var result = vkQueueSubmit(vulkanLogicalDevice.GraphicsQueue, 1, &submitInfo, VkFence.Null);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to submit queue!");
        }
    }

    private unsafe VkSubmitInfo CreateSubmitInfoUnsafe()
    {
        var pipelineStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;

        VkSubmitInfo submitInfo = VkSubmitInfo.New();
        submitInfo.pWaitDstStageMask = &pipelineStageFlags;


        return submitInfo;
    }

    public void Dispose()
    {
    }

    private unsafe void CreateSurfaceUnsafe(VkInstance vkInstance)
    {
        var result = GLFW.CreateWindowSurface(vkInstance.Handle, window.NativeWindowHandle, IntPtr.Zero, out var surfacePtr);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create window surface!");
        }

        surface = new VkSurfaceKHR((ulong)surfacePtr.ToInt64());

        presentMode = GetSurfacePresentModeUnsafe();

        surfaceFormat = GetSurfaceFormatUnsafe();
    }

    private unsafe VkPresentModeKHR GetSurfacePresentModeUnsafe()
    {
        var presentModeCount = 0u;
        var result = vkGetPhysicalDeviceSurfacePresentModesKHR(vulkanPhysicalDevice.PhysicalDevice, surface, &presentModeCount, null);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface present modes!");
        }

        var presentModes = stackalloc VkPresentModeKHR[(int)presentModeCount];
        result = vkGetPhysicalDeviceSurfacePresentModesKHR(vulkanPhysicalDevice.PhysicalDevice, surface, &presentModeCount, presentModes);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface present modes!");
        }

        var presentModeKHR = VkPresentModeKHR.FifoKHR; // If v-sync is enabled, this is the only mode available
        for (int i = 0; i < presentModeCount; i++)
        {
            if (presentModes[i].Equals(VkPresentModeKHR.MailboxKHR))
            {
                presentModeKHR = VkPresentModeKHR.MailboxKHR;
                break;
            }
        }

        return presentModeKHR;
    }

    private unsafe VkSurfaceFormatKHR GetSurfaceFormatUnsafe()
    {
        var surfaceFormatCount = 0u;
        var result = vkGetPhysicalDeviceSurfaceFormatsKHR(vulkanPhysicalDevice.PhysicalDevice, surface, &surfaceFormatCount, null);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface present modes!");
        }

        var surfaceFormats = stackalloc VkSurfaceFormatKHR[(int)surfaceFormatCount];
        result = vkGetPhysicalDeviceSurfaceFormatsKHR(vulkanPhysicalDevice.PhysicalDevice, surface, &surfaceFormatCount, surfaceFormats);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface present modes!");
        }

        var surfaceFormatKHR = surfaceFormats[0];
        for (int i = 0; i < surfaceFormatCount; i++)
        {
            var format = surfaceFormats[i];

            if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR && format.format == VkFormat.B8g8r8a8Unorm)
            {
                surfaceFormatKHR = format;
                break;
            }
        }

        return surfaceFormatKHR;
    }

    private unsafe void CreateSwapChainUnsafe()
    {
        var surfaceCapabilities = new VkSurfaceCapabilitiesKHR();
        var result = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(vulkanPhysicalDevice.PhysicalDevice, surface, out surfaceCapabilities);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface capabilities!");
        }

        var swapChainCreateInfo = VkSwapchainCreateInfoKHR.New();
        swapChainCreateInfo.surface = surface;
        swapChainCreateInfo.oldSwapchain = VkSwapchainKHR.Null;
        swapChainCreateInfo.imageFormat = surfaceFormat.format;
        swapChainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
        swapChainCreateInfo.minImageCount = surfaceCapabilities.minImageCount + 1; // frames in flight
        swapChainCreateInfo.imageSharingMode = VkSharingMode.Exclusive;
        swapChainCreateInfo.imageExtent = extent;
        swapChainCreateInfo.imageArrayLayers = 1;
        swapChainCreateInfo.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
        swapChainCreateInfo.clipped = true;
        swapChainCreateInfo.imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;
        swapChainCreateInfo.preTransform = surfaceCapabilities.currentTransform;
        swapChainCreateInfo.presentMode = presentMode;

        result = vkCreateSwapchainKHR(vulkanLogicalDevice.Device, ref swapChainCreateInfo, null, out swapchain);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create swapchain!");
        }
    }

    private unsafe void CreateImageViewsUnsafe()
    {
        // Get the images
        uint swapChainImageCount = 0;
        var result = vkGetSwapchainImagesKHR(vulkanLogicalDevice.Device, swapchain, &swapChainImageCount, null);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get swapchain images!");
        }

        var swapChainImages = stackalloc VkImage[(int)swapChainImageCount];
        result = vkGetSwapchainImagesKHR(vulkanLogicalDevice.Device, swapchain, &swapChainImageCount, swapChainImages);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get swapchain images!");
        }

        imageViews = new RawList<VkImageView>(swapChainImageCount);

        for (int i = 0; i < swapChainImageCount; i++)
        {
            var imageViewCreateInfo = VkImageViewCreateInfo.New();
            imageViewCreateInfo.image = swapChainImages[i];
            imageViewCreateInfo.viewType = VkImageViewType.Image2D;
            imageViewCreateInfo.format = surfaceFormat.format;
            imageViewCreateInfo.components.r = VkComponentSwizzle.Identity;
            imageViewCreateInfo.components.g = VkComponentSwizzle.Identity;
            imageViewCreateInfo.components.b = VkComponentSwizzle.Identity;
            imageViewCreateInfo.components.a = VkComponentSwizzle.Identity;
            imageViewCreateInfo.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            imageViewCreateInfo.subresourceRange.baseMipLevel = 0;
            imageViewCreateInfo.subresourceRange.levelCount = 1;
            imageViewCreateInfo.subresourceRange.baseArrayLayer = 0;
            imageViewCreateInfo.subresourceRange.layerCount = 1;

            var imageResult = vkCreateImageView(vulkanLogicalDevice.Device, ref imageViewCreateInfo, null, out var imageView);
            if (imageResult is not VkResult.Success)
            {
                throw new ApplicationException("Failed to create image view!");
            }

            imageViews.Add(imageView);
        }
    }

    private unsafe void CreateWaitFences()
    {
        waitFences = new RawList<VkFence>(imageViews.Count);

        for (int i = 0; i < imageViews.Count; i++)
        {
            var fenceCreateInfo = VkFenceCreateInfo.New();
            fenceCreateInfo.flags = VkFenceCreateFlags.Signaled;

            var result = vkCreateFence(vulkanLogicalDevice.Device, ref fenceCreateInfo, null, out var fence);
            if (result is not VkResult.Success)
            {
                throw new ApplicationException("Failed to create fence!");
            }

            waitFences.Add(fence);
        }
    }
}
