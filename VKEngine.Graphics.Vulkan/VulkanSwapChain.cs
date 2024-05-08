using VKEngine.Configuration;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanSwapChain(IGraphicsConfiguration graphicsConfiguration, IWindow window, IGraphicsContext graphicsContext, IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice) : ISwapChain
{
    internal VkSurfaceKHR surface = VkSurfaceKHR.Null;
    internal VkSwapchainKHR swapchain = VkSwapchainKHR.Null;
    internal VkImage[] images = [];
    internal VkImageView[] imageViews = [];
    internal VkFramebuffer[] framebuffers = [];

    internal VkSurfaceCapabilitiesKHR surfaceCapabilities;
    internal VkSurfaceFormatKHR surfaceFormat;
    internal VkPresentModeKHR presentMode;
    internal VkExtent2D extent;

    internal VkSemaphore imageAvailableSemaphore = VkSemaphore.Null;
    internal VkSemaphore renderFinishedSemaphore = VkSemaphore.Null;
    internal VkFence inFlightFence = VkFence.Null;

    private uint imageCount = 0;
    internal uint imageIndex = 0;

    private VulkanRenderPass vulkanRenderPass;
    private VkSurfaceFormatKHR[] supportedSurfaceFormats = [];
    private VkPresentModeKHR[] supportedPresentModes = [];

    public void Initialize(IRenderPass renderPass)
    {
        if (graphicsContext is not IVulkanGraphicsContext vulkanGraphicsContext)
        {
            throw new InvalidCastException("VulkanSwapChain can only be used with IVulkanGraphicsContext!");
        }

        if (renderPass is not VulkanRenderPass vulkanRenderPass)
        {
            throw new InvalidCastException("VulkanSwapChain can only be used with IVulkanRenderPass!");
        }
        this.vulkanRenderPass = vulkanRenderPass;

        CreateSurface(vulkanGraphicsContext);

        QuerySwapChainSupport(physicalDevice);

        var swapChainAdequate = supportedSurfaceFormats.Length != 0 && supportedPresentModes.Length != 0;
        if (swapChainAdequate is false)
        {
            throw new ApplicationException("Swap chain is not adequate!");
        }

        surfaceFormat = ChooseSwapSurfaceFormat();
        presentMode = ChooseSwapPresentMode();
        extent = ChooseSwapExtent();

        CreateSwapChain(vulkanRenderPass);

        CreateSynchronizationObjects(logicalDevice);
    }

    public void Cleanup()
    {
        CleanupSwapChain();

        vkDestroySemaphore(logicalDevice.Device, imageAvailableSemaphore, nint.Zero);
        imageAvailableSemaphore = VkSemaphore.Null;

        vkDestroySemaphore(logicalDevice.Device, renderFinishedSemaphore, nint.Zero);
        renderFinishedSemaphore = VkSemaphore.Null;

        vkDestroyFence(logicalDevice.Device, inFlightFence, nint.Zero);
        inFlightFence = VkFence.Null;

        if (graphicsContext is not IVulkanGraphicsContext vulkanGraphicsContext)
        {
            throw new InvalidCastException("VulkanSwapChain can only be used with IVulkanGraphicsContext!");
        }

        vkDestroySurfaceKHR(vulkanGraphicsContext.Instance.Handle, surface, nint.Zero);
    }

    private void CleanupSwapChain()
    {
        vkDeviceWaitIdle(logicalDevice.Device);

        for (var i = 0; i < framebuffers.Length; i++)
        {
            vkDestroyFramebuffer(logicalDevice.Device, framebuffers[i], nint.Zero);
            framebuffers[i] = VkFramebuffer.Null;
        }

        for (var i = 0; i < imageViews.Length; i++)
        {
            vkDestroyImageView(logicalDevice.Device, imageViews[i], nint.Zero);
            imageViews[i] = VkImageView.Null;
        }


        if (swapchain != VkSwapchainKHR.Null)
        {
            vkDestroySwapchainKHR(logicalDevice.Device, swapchain, nint.Zero);
            swapchain = VkSwapchainKHR.Null;
        }
    }

    private void CreateSurface(IVulkanGraphicsContext vulkanGraphicsContext)
    {
        var result = GLFW.CreateWindowSurface(vulkanGraphicsContext.Instance.Handle, window.NativeWindowHandle, IntPtr.Zero, out var surfacePtr);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create window surface!");
        }

        surface = new VkSurfaceKHR((ulong)surfacePtr.ToInt64());
    }

    private unsafe void CreateSwapChain(VulkanRenderPass renderPass)
    {
        imageCount = surfaceCapabilities.minImageCount + 1;
        if (surfaceCapabilities.maxImageCount > 0 && imageCount > surfaceCapabilities.maxImageCount)
        {
            imageCount = surfaceCapabilities.maxImageCount;
        }

        var swapchainCreateInfo = VkSwapchainCreateInfoKHR.New();
        swapchainCreateInfo.surface = surface;
        swapchainCreateInfo.minImageCount = imageCount;
        swapchainCreateInfo.imageFormat = surfaceFormat.format;
        swapchainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
        swapchainCreateInfo.imageExtent = extent;
        swapchainCreateInfo.imageArrayLayers = 1;
        swapchainCreateInfo.imageUsage = VkImageUsageFlags.ColorAttachment;

        var queueFamilyIndices = new uint[] { physicalDevice.QueueFamilyIndices.Graphics, physicalDevice.QueueFamilyIndices.Present };
        if (physicalDevice.QueueFamilyIndices.Graphics != physicalDevice.QueueFamilyIndices.Present)
        {
            swapchainCreateInfo.imageSharingMode = VkSharingMode.Concurrent;
            swapchainCreateInfo.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
            fixed (uint* pQueueFamilyIndices = &queueFamilyIndices[0])
            {
                swapchainCreateInfo.pQueueFamilyIndices = pQueueFamilyIndices;
            }
        }
        else
        {
            swapchainCreateInfo.imageSharingMode = VkSharingMode.Exclusive;
            swapchainCreateInfo.queueFamilyIndexCount = 0;
            swapchainCreateInfo.pQueueFamilyIndices = null;
        }

        swapchainCreateInfo.preTransform = surfaceCapabilities.currentTransform;
        swapchainCreateInfo.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
        swapchainCreateInfo.presentMode = presentMode;
        swapchainCreateInfo.clipped = true;
        swapchainCreateInfo.oldSwapchain = VkSwapchainKHR.Null;

        var result = vkCreateSwapchainKHR(logicalDevice.Device, &swapchainCreateInfo, null, out swapchain);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create swap chain!");
        }

        var swapChainImageCount = 0u;
        vkGetSwapchainImagesKHR(logicalDevice.Device, swapchain, &swapChainImageCount, null);
        if (imageCount != swapChainImageCount)
        {
            throw new ApplicationException("Failed to get swap chain images!");
        }

        images = new VkImage[swapChainImageCount];
        fixed (VkImage* pImages = &images[0])
        {
            vkGetSwapchainImagesKHR(logicalDevice.Device, swapchain, &swapChainImageCount, pImages);
        }

        imageViews = new VkImageView[swapChainImageCount];
        for (var i = 0; i < swapChainImageCount; i++)
        {
            var imageViewCreateInfo = VkImageViewCreateInfo.New();
            imageViewCreateInfo.image = images[i];
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

            if (vkCreateImageView(logicalDevice.Device, &imageViewCreateInfo, null, out imageViews[i]) is not VkResult.Success)
            {
                throw new ApplicationException("Failed to create image view!");
            }
        }

        framebuffers = new VkFramebuffer[swapChainImageCount];
        for (var i = 0; i < swapChainImageCount; i++)
        {
            var framebufferCreateInfo = VkFramebufferCreateInfo.New();
            framebufferCreateInfo.renderPass = renderPass.renderPass;
            framebufferCreateInfo.attachmentCount = 1;
            fixed (VkImageView* pImageView = &imageViews[i])
            {
                framebufferCreateInfo.pAttachments = pImageView;
            }
            framebufferCreateInfo.width = extent.width;
            framebufferCreateInfo.height = extent.height;
            framebufferCreateInfo.layers = 1;

            if (vkCreateFramebuffer(logicalDevice.Device, &framebufferCreateInfo, null, out framebuffers[i]) is not VkResult.Success)
            {
                throw new ApplicationException("Failed to create framebuffer!");
            }
        }
    }

    private unsafe void CreateSynchronizationObjects(IVulkanLogicalDevice logicalDevice)
    {
        var semaphoreCreateInfo = VkSemaphoreCreateInfo.New();
        if (vkCreateSemaphore(logicalDevice.Device, &semaphoreCreateInfo, null, out imageAvailableSemaphore) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create image available semaphore!");
        }

        if (vkCreateSemaphore(logicalDevice.Device, &semaphoreCreateInfo, null, out renderFinishedSemaphore) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create render finished semaphore!");
        }

        var fenceCreateInfo = VkFenceCreateInfo.New();
        fenceCreateInfo.flags = VkFenceCreateFlags.Signaled;
        if (vkCreateFence(logicalDevice.Device, &fenceCreateInfo, null, out inFlightFence) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create in flight fence!");
        }
    }

    private VkExtent2D ChooseSwapExtent()
    {
        if (surfaceCapabilities.currentExtent.width != uint.MaxValue)
        {
            return surfaceCapabilities.currentExtent;
        }

        var actualExtent = new VkExtent2D
        {
            width = (uint)window.Width,
            height = (uint)window.Height
        };

        actualExtent.width = Math.Clamp(actualExtent.width, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width);
        actualExtent.height = Math.Clamp(actualExtent.height, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height);

        return actualExtent;
    }

    private VkPresentModeKHR ChooseSwapPresentMode()
    {
        if (graphicsConfiguration.EnableVSync)
        {
            return VkPresentModeKHR.FifoKHR;
        }

        foreach (var supportedPresentMode in supportedPresentModes)
        {
            if (supportedPresentMode == VkPresentModeKHR.MailboxKHR)
            {
                return supportedPresentMode;
            }
        }

        return VkPresentModeKHR.FifoKHR;
    }

    private unsafe VkSurfaceFormatKHR ChooseSwapSurfaceFormat()
    {
        //    VkSurfaceFormatKHR surfaceFormat = new VkSurfaceFormatKHR();
        //    if (formats.Length == 1 && formats[0].format == VkFormat.Undefined)
        //    {
        //        surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR, format = VkFormat.B8g8r8a8Unorm };
        //    }
        //    else
        //    {
        //        foreach (VkSurfaceFormatKHR format in formats)
        //        {
        //            if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR && format.format == VkFormat.B8g8r8a8Unorm)
        //            {
        //                surfaceFormat = format;
        //                break;
        //            }
        //        }
        //        if (surfaceFormat.format == VkFormat.Undefined)
        //        {
        //            surfaceFormat = formats[0];
        //        }
        //    }

        if (supportedSurfaceFormats.Length == 1 && supportedSurfaceFormats[0].format == VkFormat.Undefined)
        {
            return new VkSurfaceFormatKHR
            {
                format = VkFormat.B8g8r8a8Unorm,
                colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR
            };
        }

        foreach (var supportedSurfaceFormat in supportedSurfaceFormats)
        {
            if (supportedSurfaceFormat.format == VkFormat.B8g8r8a8Unorm && supportedSurfaceFormat.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR)
            {
                return supportedSurfaceFormat;
            }
        }

        return supportedSurfaceFormats[0];
    }

    private unsafe void QuerySwapChainSupport(IVulkanPhysicalDevice physicalDevice)
    {
        if (vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice.PhysicalDevice, surface, out surfaceCapabilities) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface capabilities!");
        }

        var formatCount = 0u;
        if (vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice.PhysicalDevice, surface, &formatCount, null) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface formats!");
        }

        supportedSurfaceFormats = new VkSurfaceFormatKHR[formatCount];
        fixed (VkSurfaceFormatKHR* pSupportedSurfaceFormats = &supportedSurfaceFormats[0])
        {
            if (vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice.PhysicalDevice, surface, &formatCount, pSupportedSurfaceFormats) is not VkResult.Success)
            {
                throw new ApplicationException("Failed to get physical device surface formats!");
            }
        }

        var presentModeCount = 0u;
        if (vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice.PhysicalDevice, surface, &presentModeCount, null) is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface present modes!");
        }

        supportedPresentModes = new VkPresentModeKHR[presentModeCount];
        fixed (VkPresentModeKHR* pSupportedPresentModes = &supportedPresentModes[0])
        {
            if (vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice.PhysicalDevice, surface, &presentModeCount, pSupportedPresentModes) is not VkResult.Success)
            {
                throw new ApplicationException("Failed to get physical device surface present modes!");
            }
        }
    }

    public unsafe void AquireNextImage()
    {
        vkWaitForFences(logicalDevice.Device, 1, ref inFlightFence, true, ulong.MaxValue);

        var result = vkAcquireNextImageKHR(logicalDevice.Device, swapchain, ulong.MaxValue, imageAvailableSemaphore, VkFence.Null, ref imageIndex);
        if (result is VkResult.ErrorOutOfDateKHR || result is VkResult.SuboptimalKHR)
        {
            CleanupSwapChain();
            CreateSwapChain(vulkanRenderPass);
            return;
        }

        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to acquire next image!");
        }

        //if (vkAcquireNextImageKHR(logicalDevice.Device, swapchain, ulong.MaxValue, imageAvailableSemaphore, VkFence.Null, ref imageIndex) is not VkResult.Success)
        //{
        //    throw new ApplicationException("Failed to acquire next image!");
        //}

        vkResetFences(logicalDevice.Device, 1, ref inFlightFence);
    }

    public unsafe void Present()
    {
        var presentInfo = VkPresentInfoKHR.New();
        presentInfo.waitSemaphoreCount = 1;
        fixed (VkSemaphore* pRenderFinishedSemaphore = &renderFinishedSemaphore)
        {
            presentInfo.pWaitSemaphores = pRenderFinishedSemaphore;
        }
        presentInfo.swapchainCount = 1;
        fixed (VkSwapchainKHR* pSwapchains = &swapchain)
        {
            presentInfo.pSwapchains = pSwapchains;
        }

        fixed (uint* pImageIndex = &imageIndex)
        {
            presentInfo.pImageIndices = pImageIndex;
        }

        var result = vkQueuePresentKHR(logicalDevice.PresentQueue, &presentInfo);
        if (result is VkResult.ErrorOutOfDateKHR || result is VkResult.SuboptimalKHR)
        {
            CleanupSwapChain();
            CreateSwapChain(vulkanRenderPass);
            return;
        }

        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to present image!");
        }
    }
}
