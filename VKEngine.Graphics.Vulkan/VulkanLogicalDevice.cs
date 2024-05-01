using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public interface IVulkanLogicalDevice
{
    void Initialize();
}

internal sealed class VulkanLogicalDevice(IVulkanPhysicalDevice vulkanPhysicalDevice) : IVulkanLogicalDevice
{
    private VkDevice device;

    public unsafe void Initialize()
    {
        if (vulkanPhysicalDevice.IsInitialized is false)
        {
            throw new ApplicationException("Physical device must be initialized before initializing logical device!");
        }

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

        VkPhysicalDeviceFeatures deviceFeatures = new VkPhysicalDeviceFeatures();

        VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();

        fixed (VkDeviceQueueCreateInfo* qciPtr = &queueCreateInfos.Items[0])
        {
            deviceCreateInfo.pQueueCreateInfos = qciPtr;
            deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;

            deviceCreateInfo.pEnabledFeatures = &deviceFeatures;

            byte* layerNames = Strings.StandardValidationLayerName;
            deviceCreateInfo.enabledLayerCount = 1;
            deviceCreateInfo.ppEnabledLayerNames = &layerNames;

            byte* extensionNames = Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME;
            deviceCreateInfo.enabledExtensionCount = 1;
            deviceCreateInfo.ppEnabledExtensionNames = &extensionNames;

            var physicalDevice = vulkanPhysicalDevice.GetRaw<VkPhysicalDevice>();

            vkCreateDevice(physicalDevice, ref deviceCreateInfo, null, out device);
        }

        vkGetDeviceQueue(_device, _graphicsQueueIndex, 0, out _graphicsQueue);
        VkQueue q;
        vkGetDeviceQueue(_device, _presentQueueIndex, 0, out q);
        _presentQueue = q;
    }
}
