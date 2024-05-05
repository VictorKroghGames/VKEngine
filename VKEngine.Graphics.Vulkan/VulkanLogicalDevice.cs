using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanLogicalDevice
{
    bool IsInitialized { get; }

    VkDevice Device { get; }
    VkQueue GraphicsQueue { get; }
    VkQueue PresentQueue { get; }

    void Initialize(IVulkanPhysicalDevice physicalDevice);
    void Cleanup();
    void WaitIdle();
}

internal sealed class VulkanLogicalDevice : IVulkanLogicalDevice
{
    private VkDevice device;
    private VkQueue graphicsQueue;
    private VkQueue presentQueue;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public VkDevice Device => device;
    public VkQueue GraphicsQueue => graphicsQueue;
    public VkQueue PresentQueue => presentQueue;

    public void Initialize(IVulkanPhysicalDevice physicalDevice)
    {
        if (physicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

        var deviceFeatures = new VkPhysicalDeviceFeatures();
        CreateLogicalDeviceUnsafe(physicalDevice, deviceFeatures);

        vkGetDeviceQueue(device, physicalDevice.QueueFamilyIndices.Graphics, 0, out graphicsQueue);
        vkGetDeviceQueue(device, physicalDevice.QueueFamilyIndices.Present, 0, out presentQueue);

        isInitialized = true;
    }

    public void Cleanup()
    {
        vkDeviceWaitIdle(device);

        vkDestroyDevice(device, IntPtr.Zero);
    }

    public void WaitIdle()
    {
        vkDeviceWaitIdle(device);
    }

    private unsafe VkResult CreateLogicalDeviceUnsafe(IVulkanPhysicalDevice physicalDevice, VkPhysicalDeviceFeatures physicalDeviceFeatures)
    {
        var familyIndices = new HashSet<uint>()
        {
            physicalDevice.QueueFamilyIndices.Graphics,
            physicalDevice.QueueFamilyIndices.Present
        };

        var queueCreateInfos = new RawList<VkDeviceQueueCreateInfo>();

        // Create logical device
        foreach (uint index in familyIndices)
        {
            VkDeviceQueueCreateInfo queueCreateInfo = VkDeviceQueueCreateInfo.New();
            queueCreateInfo.queueFamilyIndex = physicalDevice.QueueFamilyIndices.Graphics;
            queueCreateInfo.queueCount = 1;
            float priority = 1f;
            queueCreateInfo.pQueuePriorities = &priority;
            queueCreateInfos.Add(queueCreateInfo);
        }

        VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();

        fixed (VkDeviceQueueCreateInfo* qciPtr = &queueCreateInfos.Items[0])
            deviceCreateInfo.pQueueCreateInfos = qciPtr;
        deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;

        deviceCreateInfo.pEnabledFeatures = &physicalDeviceFeatures;

        byte* layerNames = Strings.StandardValidationLayerName;
        deviceCreateInfo.enabledLayerCount = 1;
        deviceCreateInfo.ppEnabledLayerNames = &layerNames;

        var supportedExtensions = GetExtensions(physicalDevice);
        fixed (IntPtr* extensionsPtr = supportedExtensions.Items)
        {
            deviceCreateInfo.enabledExtensionCount = (uint)supportedExtensions.Count;
            deviceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsPtr;
        }

        return vkCreateDevice(physicalDevice.PhysicalDevice, ref deviceCreateInfo, null, out device);
    }

    private RawList<IntPtr> GetExtensions(IVulkanPhysicalDevice physicalDevice)
    {
        var extensions = new RawList<IntPtr>();

        if (physicalDevice.IsExtensionSupported("VK_KHR_swapchain"))
        {
            extensions.Add(Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);
        }

        return extensions;
    }
}
