using VKEngine.Graphics.Vulkan.Native;
using Vulkan;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanSwapChain
{
    void Initialize(VkInstance vkInstance);
}

internal class VulkanSwapChain(IWindow window) : IVulkanSwapChain
{
    private VkSurfaceKHR surface;

    public void Initialize(VkInstance vkInstance)
    {
        CreateSurface(vkInstance);
    }

    private void CreateSurface(VkInstance vkInstance)
    {
        var result = GLFW.CreateWindowSurface(vkInstance.Handle, window.NativeWindowHandle, IntPtr.Zero, out var surfacePtr);
        if(result is not VkResult.Success)
        {
            throw new ApplicationException("Failed to create window surface!");
        }

        surface = new VkSurfaceKHR((ulong)surfacePtr.ToInt64());
    }
}
