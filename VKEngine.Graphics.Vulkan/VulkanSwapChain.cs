using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanSwapChain
{
    void Initialize(VkInstance vkInstance);
}

internal class VulkanSwapChain(IWindow window, IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanSwapChain
{
    private VkSurfaceKHR _surface;

    // Swapchain stuff
    private RawList<VkImage> _scImages = new RawList<VkImage>();
    private RawList<VkImageView> _scImageViews = new RawList<VkImageView>();
    private RawList<VkFramebuffer> _scFramebuffers = new RawList<VkFramebuffer>();
    private VkSwapchainKHR _swapchain;
    private VkFormat _scImageFormat;
    private VkExtent2D _scExtent;

    public void Initialize(VkInstance vkInstance)
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

        CreateSurface(vkInstance);
        CreateSwapChainUnsafe();
        CreateImageViews();
    }

    private void CreateSurface(VkInstance vkInstance)
    {
        var result = GLFW.CreateWindowSurface(vkInstance.Handle, window.NativeWindowHandle, IntPtr.Zero, out var surfacePtr);
        if(result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create window surface!");
        }

        _surface = new VkSurfaceKHR((ulong)surfacePtr.ToInt64());
    }

    private unsafe void CreateSwapChainUnsafe()
    {
        uint surfaceFormatCount = 0;
        vkGetPhysicalDeviceSurfaceFormatsKHR(vulkanPhysicalDevice.PhysicalDevice, _surface, ref surfaceFormatCount, null);
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
        vkGetPhysicalDeviceSurfaceFormatsKHR(vulkanPhysicalDevice.PhysicalDevice, _surface, ref surfaceFormatCount, out formats[0]);

        VkSurfaceFormatKHR surfaceFormat = new VkSurfaceFormatKHR();
        if (formats.Length == 1 && formats[0].format == VkFormat.Undefined)
        {
            surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR, format = VkFormat.B8g8r8a8Unorm };
        }
        else
        {
            foreach (VkSurfaceFormatKHR format in formats)
            {
                if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR && format.format == VkFormat.B8g8r8a8Unorm)
                {
                    surfaceFormat = format;
                    break;
                }
            }
            if (surfaceFormat.format == VkFormat.Undefined)
            {
                surfaceFormat = formats[0];
            }
        }

        uint presentModeCount = 0;
        vkGetPhysicalDeviceSurfacePresentModesKHR(vulkanPhysicalDevice.PhysicalDevice, _surface, ref presentModeCount, null);
        VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
        vkGetPhysicalDeviceSurfacePresentModesKHR(vulkanPhysicalDevice.PhysicalDevice, _surface, ref presentModeCount, out presentModes[0]);

        VkPresentModeKHR presentMode = VkPresentModeKHR.FifoKHR;
        if (presentModes.Contains(VkPresentModeKHR.MailboxKHR))
        {
            presentMode = VkPresentModeKHR.MailboxKHR;
        }
        else if (presentModes.Contains(VkPresentModeKHR.ImmediateKHR))
        {
            presentMode = VkPresentModeKHR.ImmediateKHR;
        }

        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(vulkanPhysicalDevice.PhysicalDevice, _surface, out VkSurfaceCapabilitiesKHR surfaceCapabilities);
        uint imageCount = surfaceCapabilities.minImageCount + 1;

        VkSwapchainCreateInfoKHR sci = VkSwapchainCreateInfoKHR.New();
        sci.surface = _surface;
        sci.presentMode = presentMode;
        sci.imageFormat = surfaceFormat.format;
        sci.imageColorSpace = surfaceFormat.colorSpace;
        sci.imageExtent = new VkExtent2D { width = (uint)window.Width, height = (uint)window.Height };
        sci.minImageCount = imageCount;
        sci.imageArrayLayers = 1;
        sci.imageUsage = VkImageUsageFlags.ColorAttachment;

        var queueFamilyIndices = vulkanPhysicalDevice.QueueFamilyIndices;

        if (vulkanPhysicalDevice.QueueFamilyIndices.Graphics != vulkanPhysicalDevice.QueueFamilyIndices.Present)
        {
            uint first = vulkanPhysicalDevice.QueueFamilyIndices.Graphics;

            sci.imageSharingMode = VkSharingMode.Concurrent;
            sci.queueFamilyIndexCount = 2;
            sci.pQueueFamilyIndices = &first;
        }
        else
        {
            sci.imageSharingMode = VkSharingMode.Exclusive;
            sci.queueFamilyIndexCount = 0;
        }

        sci.preTransform = surfaceCapabilities.currentTransform;
        sci.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
        sci.clipped = true;

        VkSwapchainKHR oldSwapchain = _swapchain;
        sci.oldSwapchain = oldSwapchain;

        vkCreateSwapchainKHR(vulkanLogicalDevice.Device, ref sci, null, out _swapchain);
        if (oldSwapchain != 0)
        {
            vkDestroySwapchainKHR(vulkanLogicalDevice.Device, oldSwapchain, null);
        }

        // Get the images
        uint scImageCount = 0;
        vkGetSwapchainImagesKHR(vulkanLogicalDevice.Device, _swapchain, ref scImageCount, null);
        _scImages.Count = scImageCount;
        vkGetSwapchainImagesKHR(vulkanLogicalDevice.Device, _swapchain, ref scImageCount, out _scImages.Items[0]);

        _scImageFormat = surfaceFormat.format;
        _scExtent = sci.imageExtent;
    }

    private void CreateImageViews()
    {
        _scImageViews.Resize(_scImages.Count);
        for (int i = 0; i < _scImages.Count; i++)
        {
            CreateImageViewUnsafe(_scImages[i], _scImageFormat, out _scImageViews[i]);
        }
    }

    private unsafe void CreateImageViewUnsafe(VkImage image, VkFormat format, out VkImageView imageView)
    {
        VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
        imageViewCI.image = image;
        imageViewCI.viewType = VkImageViewType.Image2D;
        imageViewCI.format = format;
        imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
        imageViewCI.subresourceRange.baseMipLevel = 0;
        imageViewCI.subresourceRange.levelCount = 1;
        imageViewCI.subresourceRange.baseArrayLayer = 0;
        imageViewCI.subresourceRange.layerCount = 1;

        vkCreateImageView(vulkanLogicalDevice.Device, ref imageViewCI, null, out imageView);
    }
}
