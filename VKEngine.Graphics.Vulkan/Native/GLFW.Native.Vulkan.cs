using System.Runtime.InteropServices;
using Vulkan;

namespace VKEngine.Graphics.Vulkan.Native;

internal partial class GLFW
{
    internal static partial class Native
    {
        public const string LibraryName = "libs/glfw3.dll";

        internal static partial class Vulkan
        {
            [LibraryImport(LibraryName, EntryPoint = "glfwVulkanSupported")]
            [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool glfwVulkanSupported();


            [LibraryImport(LibraryName, EntryPoint = "glfwGetRequiredInstanceExtensions")]
            [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
            internal static partial IntPtr glfwGetRequiredInstanceExtensions(
                out uint count);

            [LibraryImport(LibraryName, EntryPoint = "glfwGetInstanceProcAddress")]
            [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
            internal static partial IntPtr glfwGetInstanceProcAddress(
                IntPtr instance,
                [MarshalAs(UnmanagedType.LPStr)] string procName);

            [LibraryImport(LibraryName, EntryPoint = "glfwGetPhysicalDevicePresentationSupport")]
            [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool glfwGetPhysicalDevicePresentationSupport(
                IntPtr instance,
                IntPtr device,
                uint queuefamily);

            [LibraryImport(LibraryName, EntryPoint = "glfwCreateWindowSurface")]
            [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
            internal static partial VkResult glfwCreateWindowSurface(
                IntPtr instance,
                IntPtr window,
                IntPtr allocator,
                out IntPtr surface);
        }
    }
}

