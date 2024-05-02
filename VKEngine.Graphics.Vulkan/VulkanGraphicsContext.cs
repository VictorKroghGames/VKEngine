using System.Runtime.InteropServices;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanGraphicsContext(IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice, IVulkanSwapChain vulkanSwapChain) : IGraphicsContext
{
    private VkInstance instance;

    public void Initialize()
    {
        if(GLFW.VulkanSupported() is false)
        {
            throw new PlatformNotSupportedException("Vulkan is not supported on this platform.");
        }

        var result = CreateInstanceUnsafe();
        if (result is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create Vulkan instance");
        }

        vulkanPhysicalDevice.Initialize(instance);
        vulkanLogicalDevice.Initialize();
        vulkanSwapChain.Initialize(instance);
    }

    private unsafe VkResult CreateInstanceUnsafe()
    {
        var applicationInfo = VkApplicationInfo.New();
        applicationInfo.pApplicationName = Strings.AppName;
        applicationInfo.pEngineName = Strings.EngineName;
        applicationInfo.engineVersion = new Version(1, 0, 0);
        applicationInfo.apiVersion = new Version(1, 0, 0);

        var instanceExtensions = GetInstanceExtensions();
        var instanceLayers = GetInstanceLayers();

        var instanceCreateInfo = VkInstanceCreateInfo.New();
        instanceCreateInfo.pApplicationInfo = &applicationInfo;

#if DEBUG
        instanceExtensions.Add(Strings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
        instanceLayers.Add(Strings.StandardValidationLayerName);
#endif

        fixed (IntPtr* extensionsBase = &instanceExtensions.ToArray()[0])
        {
            instanceCreateInfo.enabledExtensionCount = (uint)instanceExtensions.Count();
            instanceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsBase;
        }

        fixed (IntPtr* layersBase = &instanceLayers.ToArray()[0])
        {
            instanceCreateInfo.enabledLayerCount = (uint)instanceLayers.Count();
            instanceCreateInfo.ppEnabledLayerNames = (byte**)layersBase;
        }

        return vkCreateInstance(&instanceCreateInfo, null, out instance);
    }

    private RawList<IntPtr> GetInstanceLayers()
    {
        return [];
    }

    private RawList<IntPtr> GetInstanceExtensions()
    {
        var glfwExtensionsPtr = GLFW.GetRequiredInstanceExtensions(out var glfwExtensionCount);
        if (glfwExtensionCount == 0)
        {
            return [];
        }

        var rawList = new RawList<IntPtr>(glfwExtensionCount);
        var offset = 0;
        for (var i = 0; i < glfwExtensionCount; i++, offset += IntPtr.Size)
        {
            var p = Marshal.ReadIntPtr(glfwExtensionsPtr, offset);
            var extension = Marshal.PtrToStringAnsi(p);
            if (string.IsNullOrWhiteSpace(extension))
            {
                continue;
            }
            rawList.Add(new FixedUtf8String(extension));
        }
        return rawList;
    }
}
