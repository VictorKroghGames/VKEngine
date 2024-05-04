using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanSwapChain : ISwapChain, IDisposable
{
    VkSwapchainKHR Raw { get; }
    VkSurfaceFormatKHR SurfaceFormat { get; }
    RawList<VkImageView> ImageViews { get; }
    VkExtent2D Extent { get; }

    void Initialize(VkInstance vkInstance);

    void Resize(int width, int height);
}

internal struct VulkanSwapChainCommandBuffer
{
    public VkCommandPool commandPool;
    public VkCommandBuffer commandBuffer;
}

internal class VulkanSwapChain(IWindow window, IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice) : IVulkanSwapChain
{
    private VkSwapchainKHR swapchain;
    private VkSurfaceKHR surface;
    private VkPresentModeKHR presentMode;
    private VkSurfaceFormatKHR surfaceFormat;
    private VkExtent2D extent = new VkExtent2D(window.Width, window.Height);
    private VkSemaphore presentCompleteSemaphore;
    private VkSemaphore renderCompleteSemaphore;

    private VulkanSwapChainCommandBuffer[] commandBuffers = [];
    private VkFence[] waitFences = [];

    private uint currentImageIndex = 0;
    private uint currentBufferIndex = 0;
    private uint framesInFlight = 2;

    private RawList<VkImageView> imageViews = [];

    public VkSwapchainKHR Raw => swapchain;
    public VkSurfaceFormatKHR SurfaceFormat => surfaceFormat;
    public RawList<VkImageView> ImageViews => imageViews;
    public VkExtent2D Extent => extent;

    public uint CurrentImageIndex => currentImageIndex;

    public ICommandBuffer CurrentCommandBuffer => new VulkanCommandBuffer(commandBuffers[currentBufferIndex].commandBuffer);

    public void Initialize(VkInstance vkInstance)
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

        CreateSurfaceUnsafe(vkInstance);
        CreateSwapChainUnsafe();
    }

    public unsafe void AquireNextImage()
    {
        uint imageIndex = 0;
        VkResult result = vkAcquireNextImageKHR(vulkanLogicalDevice.Device, swapchain, ulong.MaxValue, presentCompleteSemaphore, VkFence.Null, ref imageIndex);
        if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
        {
            Resize(0, 0);
        }
        else if (result != VkResult.Success)
        {
            throw new InvalidOperationException("Acquiring next image failed: " + result);
        }

        currentImageIndex = imageIndex;

        vkResetCommandPool(vulkanLogicalDevice.Device, commandBuffers[currentBufferIndex].commandPool, VkCommandPoolResetFlags.None);
    }

    public unsafe void Present()
    {
        var swapchainPresent = swapchain;
        var imageIndex = currentImageIndex;

        var waitSemaphore = presentCompleteSemaphore;
        var signalSemaphore = renderCompleteSemaphore;
        var commandBuffer = commandBuffers[currentBufferIndex].commandBuffer;
        var waitStages = VkPipelineStageFlags.ColorAttachmentOutput;

        var submitInfo = VkSubmitInfo.New();
        submitInfo.waitSemaphoreCount = 1;
        submitInfo.pWaitSemaphores = &waitSemaphore;
        submitInfo.pWaitDstStageMask = &waitStages;
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &commandBuffer;
        submitInfo.signalSemaphoreCount = 1;
        submitInfo.pSignalSemaphores = &signalSemaphore;

        vkResetFences(vulkanLogicalDevice.Device, 1, ref waitFences[currentBufferIndex]);
        vkQueueSubmit(vulkanLogicalDevice.GraphicsQueue, 1, &submitInfo, waitFences[currentBufferIndex]);

        VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
        presentInfo.waitSemaphoreCount = 1;
        presentInfo.pWaitSemaphores = &signalSemaphore;

        presentInfo.swapchainCount = 1;
        presentInfo.pSwapchains = &swapchainPresent;
        presentInfo.pImageIndices = &imageIndex;

        vkQueuePresentKHR(vulkanLogicalDevice.PresentQueue, ref presentInfo);

        currentBufferIndex = (currentBufferIndex + 1) % framesInFlight; // 2 = frames in flight
        vkWaitForFences(vulkanLogicalDevice.Device, 1, ref waitFences[currentBufferIndex], true, ulong.MaxValue);
    }

    public void Resize(int width, int height)
    {
        vulkanLogicalDevice.WaitIdle();

        CreateSwapChainUnsafe();

        vulkanLogicalDevice.WaitIdle();
    }

    public void Dispose()
    {
        vulkanLogicalDevice.WaitIdle();

        //vkDestroySurfaceKHR(((IVulkanGraphicsContext)graphicsContext).Instance, surface, IntPtr.Zero);
        vkDestroySwapchainKHR(vulkanLogicalDevice.Device, swapchain, IntPtr.Zero);

        for (int i = 0; i < imageViews.Count; i++)
        {
            vkDestroyImageView(vulkanLogicalDevice.Device, imageViews[i], IntPtr.Zero);
        }

        foreach (var commandBuffer in commandBuffers)
        {
            vkDestroyCommandPool(vulkanLogicalDevice.Device, commandBuffer.commandPool, IntPtr.Zero);
        }

        vkDestroySemaphore(vulkanLogicalDevice.Device, presentCompleteSemaphore, IntPtr.Zero);
        vkDestroySemaphore(vulkanLogicalDevice.Device, renderCompleteSemaphore, IntPtr.Zero);

        for (int i = 0; i < waitFences.Length; i++)
        {
            vkDestroyFence(vulkanLogicalDevice.Device, waitFences[i], IntPtr.Zero);
        }

        vulkanLogicalDevice.WaitIdle();
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
        var result = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(vulkanPhysicalDevice.PhysicalDevice, surface, out var surfaceCapabilities);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to get physical device surface capabilities!");
        }

        var swapChainExtent = surfaceCapabilities.currentExtent;

        var oldSwapChain = swapchain;

        var swapChainCreateInfo = VkSwapchainCreateInfoKHR.New();
        swapChainCreateInfo.pNext = null;
        swapChainCreateInfo.surface = surface;
        swapChainCreateInfo.oldSwapchain = VkSwapchainKHR.Null;
        swapChainCreateInfo.imageFormat = surfaceFormat.format;
        swapChainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
        swapChainCreateInfo.minImageCount = surfaceCapabilities.minImageCount + 1; // frames in flight
        swapChainCreateInfo.imageSharingMode = VkSharingMode.Exclusive;
        swapChainCreateInfo.imageExtent = swapChainExtent; // extent;
        swapChainCreateInfo.imageArrayLayers = 1;
        swapChainCreateInfo.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
        swapChainCreateInfo.clipped = true;
        swapChainCreateInfo.imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;
        swapChainCreateInfo.preTransform = surfaceCapabilities.currentTransform;
        swapChainCreateInfo.presentMode = presentMode;
        swapChainCreateInfo.oldSwapchain = oldSwapChain;

        result = vkCreateSwapchainKHR(vulkanLogicalDevice.Device, ref swapChainCreateInfo, null, out swapchain);
        if (result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create swapchain!");
        }

        if (oldSwapChain != VkSwapchainKHR.Null)
        {
            vkDestroySwapchainKHR(vulkanLogicalDevice.Device, oldSwapChain, IntPtr.Zero);
        }

        for (int i = 0; i < imageViews.Count; i++)
        {
            vkDestroyImageView(vulkanLogicalDevice.Device, imageViews[i], IntPtr.Zero);
        }

        CreateImageViewsUnsafe();

        foreach (var commandBuffer in commandBuffers)
        {
            vkDestroyCommandPool(vulkanLogicalDevice.Device, commandBuffer.commandPool, null);
        }

        CreateCommandBuffersUnsafe(vulkanLogicalDevice);

        CreateSemaphoresUnsafe();

        if(waitFences.Length != imageViews.Count)
        {
            var fenceCreateInfo = VkFenceCreateInfo.New();
            fenceCreateInfo.flags = VkFenceCreateFlags.Signaled;

            waitFences = new VkFence[imageViews.Count];
            for (int i = 0; i < waitFences.Length; i++)
            {
                vkCreateFence(vulkanLogicalDevice.Device, ref fenceCreateInfo, IntPtr.Zero, out waitFences[i]);
            }
        }
    }

    private unsafe void CreateCommandBuffersUnsafe(IVulkanLogicalDevice vulkanLogicalDevice)
    {
        var commandPoolCreateInfo = VkCommandPoolCreateInfo.New();
        commandPoolCreateInfo.queueFamilyIndex = vulkanPhysicalDevice.QueueFamilyIndices.Graphics;
        commandPoolCreateInfo.flags = VkCommandPoolCreateFlags.Transient;

        var commandBufferAllocateInfo = VkCommandBufferAllocateInfo.New();
        commandBufferAllocateInfo.commandPool = VkCommandPool.Null;
        commandBufferAllocateInfo.level = VkCommandBufferLevel.Primary;
        commandBufferAllocateInfo.commandBufferCount = 1;

        commandBuffers = new VulkanSwapChainCommandBuffer[imageViews.Count];
        for (int i = 0; i < commandBuffers.Length; i++)
        {
            vkCreateCommandPool(vulkanLogicalDevice.Device, &commandPoolCreateInfo, IntPtr.Zero, out commandBuffers[i].commandPool);

            commandBufferAllocateInfo.commandPool = commandBuffers[i].commandPool;
            vkAllocateCommandBuffers(vulkanLogicalDevice.Device, &commandBufferAllocateInfo, out commandBuffers[i].commandBuffer);
        }
        //foreach (var commandBuffer in commandBuffers)
        //{
        //    vkCreateCommandPool(vulkanLogicalDevice.Device, &commandPoolCreateInfo, IntPtr.Zero, &commandBuffer.commandPool);

        //    commandBufferAllocateInfo.commandPool = commandBuffer.commandPool;
        //    vkAllocateCommandBuffers(vulkanLogicalDevice.Device, &commandBufferAllocateInfo, &commandBuffer.commandBuffer);
        //}
    }

    private unsafe void CreateSemaphoresUnsafe()
    {
        VkSemaphoreCreateInfo semaphoreCI = VkSemaphoreCreateInfo.New();
        vkCreateSemaphore(vulkanLogicalDevice.Device, ref semaphoreCI, null, out presentCompleteSemaphore);
        vkCreateSemaphore(vulkanLogicalDevice.Device, ref semaphoreCI, null, out renderCompleteSemaphore);
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
}
