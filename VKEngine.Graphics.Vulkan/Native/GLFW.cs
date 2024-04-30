using Vulkan;

namespace VKEngine.Graphics.Vulkan.Native;

internal partial class GLFW
{
    internal static bool VulkanSupported() => Native.Vulkan.glfwVulkanSupported();
    internal static IntPtr GetRequiredInstanceExtensions(out uint count) => Native.Vulkan.glfwGetRequiredInstanceExtensions(out count);
    internal static IntPtr GetInstanceProcAddress(IntPtr instance, string procName) => Native.Vulkan.glfwGetInstanceProcAddress(instance, procName);
    internal static bool GetPhysicalDevicePresentationSupport(IntPtr instance, IntPtr device, uint queuefamily) => Native.Vulkan.glfwGetPhysicalDevicePresentationSupport(instance, device, queuefamily);
    internal static VkResult CreateWindowSurface(IntPtr instance, IntPtr window, IntPtr allocator, out IntPtr surface) => Native.Vulkan.glfwCreateWindowSurface(instance, window, allocator, out surface);
}

