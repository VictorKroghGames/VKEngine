using VKEngine.Graphics.Vulkan.Native;

namespace VKEngine.Graphics.Vulkan;

internal static class Strings
{
    public static FixedUtf8String AppName { get; } = "VKEngine";
    public static FixedUtf8String EngineName { get; } = "VKEngine";
    public static FixedUtf8String VK_KHR_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_surface";
    public static FixedUtf8String VK_KHR_WIN32_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_win32_surface";
    public static FixedUtf8String VK_KHR_XCB_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_xcb_surface";
    public static FixedUtf8String VK_KHR_XLIB_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_xlib_surface";
    public static FixedUtf8String VK_KHR_SWAPCHAIN_EXTENSION_NAME { get; } = "VK_KHR_swapchain";
    public static FixedUtf8String VK_EXT_DEBUG_REPORT_EXTENSION_NAME { get; } = "VK_EXT_debug_report";
    public static FixedUtf8String StandardValidationLayerName { get; } = "VK_LAYER_KHRONOS_validation"; // "VK_LAYER_LUNARG_standard_validation"; 
    public static FixedUtf8String main { get; } = "main";
}
