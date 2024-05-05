using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

public struct QueueFamilyIndex
{
    public uint Graphics { get; set; }
    public uint Present { get; set; }
}

public interface IVulkanPhysicalDevice
{
    bool IsInitialized { get; }

    VkPhysicalDevice PhysicalDevice { get; }
    QueueFamilyIndex QueueFamilyIndices { get; }

    void Initialize(VkInstance vkInstance);
    void Cleanup();

    bool IsExtensionSupported(string extensionName);
}

internal class VulkanPhysicalDevice : IVulkanPhysicalDevice
{
    internal VkPhysicalDevice physicalDevice = VkPhysicalDevice.Null;
    private VkPhysicalDeviceProperties physicalDeviceProperties;
    private VkPhysicalDeviceFeatures physicalDeviceFeatures;
    private QueueFamilyIndex queueFamilyIndices;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;
    public VkPhysicalDevice PhysicalDevice => physicalDevice;
    public QueueFamilyIndex QueueFamilyIndices => queueFamilyIndices;

    public void Initialize(VkInstance vkInstance)
    {
        var physicalDevices = GetVkPhysicalDevicesUnsafe(vkInstance);

        var mostSuitablePhysicalDevice = SelectMostSuitablePhysicalDevice(physicalDevices);
        if (mostSuitablePhysicalDevice == VkPhysicalDevice.Null)
        {
            throw new ApplicationException("Failed to find a suitable physical device!");
        }

        physicalDevice = mostSuitablePhysicalDevice;

        GetPhysicalDevicePropertiesAndFeaturesUnsafe();

        GetQueueFamiliesUnsafe();

        isInitialized = physicalDevice != VkPhysicalDevice.Null;
    }

    public void Cleanup()
    {
    }

    private unsafe VkPhysicalDevice[] GetVkPhysicalDevicesUnsafe(VkInstance vkInstance)
    {
        uint physicalDeviceCount = 0u;
        vkEnumeratePhysicalDevices(vkInstance, &physicalDeviceCount, null);

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        vkEnumeratePhysicalDevices(vkInstance, &physicalDeviceCount, physicalDevices);

        var result = new VkPhysicalDevice[physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            result[i] = physicalDevices[i];
        }
        return result;
    }

    private VkPhysicalDevice SelectMostSuitablePhysicalDevice(IEnumerable<VkPhysicalDevice> physicalDevices)
    {
        var physicalDeviceScores = new Dictionary<VkPhysicalDevice, uint>();

        foreach (var physicalDevice in physicalDevices)
        {
            uint score = RatePhysicalDeviceSuitabilityUnsafe(physicalDevice);
            physicalDeviceScores.Add(physicalDevice, score);
        }

        var mostSuitablePhysicalDevice = physicalDeviceScores.OrderByDescending(x => x.Value).FirstOrDefault().Key;

        return mostSuitablePhysicalDevice;
    }

    private unsafe uint RatePhysicalDeviceSuitabilityUnsafe(VkPhysicalDevice physicalDevice)
    {
        uint score = 0;

        VkPhysicalDeviceProperties deviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &deviceProperties);

        VkPhysicalDeviceFeatures deviceFeatures;
        vkGetPhysicalDeviceFeatures(physicalDevice, &deviceFeatures);

        if (deviceProperties.deviceType == VkPhysicalDeviceType.DiscreteGpu)
        {
            score += 1000;
        }

        score += deviceProperties.limits.maxImageDimension2D;

        return score;
    }

    private unsafe void GetPhysicalDevicePropertiesAndFeaturesUnsafe()
    {
        vkGetPhysicalDeviceProperties(physicalDevice, out physicalDeviceProperties);
        vkGetPhysicalDeviceFeatures(physicalDevice, out physicalDeviceFeatures);
    }

    private unsafe void GetQueueFamiliesUnsafe()
    {
        uint queueFamilyCount = 0u;
        vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);

        VkQueueFamilyProperties* queueFamilyProperties = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
        vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilyProperties);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            var queueFamily = queueFamilyProperties[i];
            if (queueFamily.queueFlags.HasFlag(VkQueueFlags.Graphics))
            {
                queueFamilyIndices.Graphics = i;
            }
        }

        queueFamilyIndices.Present = queueFamilyIndices.Graphics;
    }

    public unsafe bool IsExtensionSupported(string extensionName)
    {
        var extensionCount = 0u;
        vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &extensionCount, null);

        var extensionProperties = stackalloc VkExtensionProperties[(int)extensionCount];
        vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &extensionCount, &extensionProperties[0]);

        for (int i = 0; i < extensionCount; i++)
        {
            var extensionProperty = extensionProperties[i];

            var str = System.Runtime.InteropServices.Marshal.PtrToStringUTF8((IntPtr)extensionProperty.extensionName);
            if(string.IsNullOrWhiteSpace(str))
            {
                continue;
            }

            if (str.Equals(extensionName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
