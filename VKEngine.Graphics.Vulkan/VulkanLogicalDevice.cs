using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanLogicalDevice : IDisposable
{
    bool IsInitialized { get; }

    VkDevice Device { get; }
    VkQueue GraphicsQueue { get; }
    VkQueue PresentQueue { get; }

    void Initialize();
    void WaitIdle();
}

internal sealed class VulkanLogicalDevice(IVulkanPhysicalDevice vulkanPhysicalDevice) : IVulkanLogicalDevice
{
    private VkDevice device;
    private VkQueue graphicsQueue;
    private VkQueue presentQueue;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    public VkDevice Device => device;
    public VkQueue GraphicsQueue => graphicsQueue;
    public VkQueue PresentQueue => presentQueue;

    public void Initialize()
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

        var deviceFeatures = new VkPhysicalDeviceFeatures();
        deviceFeatures.samplerAnisotropy = false;
        CreateLogicalDeviceUnsafe(vulkanPhysicalDevice, deviceFeatures);

        vkGetDeviceQueue(device, vulkanPhysicalDevice.QueueFamilyIndices.Graphics, 0, out graphicsQueue);
        vkGetDeviceQueue(device, vulkanPhysicalDevice.QueueFamilyIndices.Present, 0, out presentQueue);

        isInitialized = true;
    }

    public void WaitIdle()
    {
        vkDeviceWaitIdle(device);
    }

    public void Dispose()
    {
        vkDestroyDevice(device, IntPtr.Zero);
    }

    private unsafe VkResult CreateLogicalDeviceUnsafe(IVulkanPhysicalDevice vulkanPhysicalDevice, VkPhysicalDeviceFeatures vkPhysicalDeviceFeatures)
    {
        var familyIndices = new HashSet<uint>()
        {
            vulkanPhysicalDevice.QueueFamilyIndices.Graphics,
            vulkanPhysicalDevice.QueueFamilyIndices.Present
        };

        var queueCreateInfos = new RawList<VkDeviceQueueCreateInfo>();

        // Create logical device
        foreach (uint index in familyIndices)
        {
            VkDeviceQueueCreateInfo queueCreateInfo = VkDeviceQueueCreateInfo.New();
            queueCreateInfo.queueFamilyIndex = vulkanPhysicalDevice.QueueFamilyIndices.Graphics;
            queueCreateInfo.queueCount = 1;
            float priority = 1f;
            queueCreateInfo.pQueuePriorities = &priority;
            queueCreateInfos.Add(queueCreateInfo);
        }

        VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();

        fixed (VkDeviceQueueCreateInfo* qciPtr = &queueCreateInfos.Items[0])
            deviceCreateInfo.pQueueCreateInfos = qciPtr;
        deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;

        deviceCreateInfo.pEnabledFeatures = &vkPhysicalDeviceFeatures;

        byte* layerNames = Strings.StandardValidationLayerName;
        deviceCreateInfo.enabledLayerCount = 1;
        deviceCreateInfo.ppEnabledLayerNames = &layerNames;

        byte* extensionNames = Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME;
        deviceCreateInfo.enabledExtensionCount = 1;
        deviceCreateInfo.ppEnabledExtensionNames = &extensionNames;

        var physicalDevice = ((VulkanPhysicalDevice)vulkanPhysicalDevice).physicalDevice;

        return vkCreateDevice(physicalDevice, ref deviceCreateInfo, null, out device);
    }
}
