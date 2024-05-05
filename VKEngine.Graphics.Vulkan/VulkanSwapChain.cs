using VKEngine.Configuration;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal class VulkanSwapChain(IGraphicsConfiguration graphicsConfiguration, IWindow window, IGraphicsContext graphicsContext) : ISwapChain
{
    internal VkSurfaceKHR surface;

    public void Initialize()
    {
        if(graphicsContext is not IVulkanGraphicsContext vulkanGraphicsContext)
        {
            throw new InvalidCastException("VulkanSwapChain can only be used with IVulkanGraphicsContext!");
        }

        CreateSurface(vulkanGraphicsContext);
    }

    public void Cleanup()
    {
        if (graphicsContext is not IVulkanGraphicsContext vulkanGraphicsContext)
        {
            throw new InvalidCastException("VulkanSwapChain can only be used with IVulkanGraphicsContext!");
        }

        vkDestroySurfaceKHR(vulkanGraphicsContext.Instance.Handle, surface, nint.Zero);
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
}
