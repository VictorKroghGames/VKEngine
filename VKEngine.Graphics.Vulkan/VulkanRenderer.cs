using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal struct QueueFamilyIndices
{
    public uint Graphics;
    public uint Compute;
    public uint Transfer;
}

internal sealed class VulkanRenderer : IRenderer
{
    private VkInstance Instance { get; set; }
    public VkPhysicalDevice PhysicalDevice { get; set; }
    public VkDevice Device { get; set; }
    public VkPhysicalDeviceMemoryProperties DeviceMemoryProperties { get; set; }
    public VkPhysicalDeviceProperties DeviceProperties { get; set; }
    public VkPhysicalDeviceFeatures DeviceFeatures { get; set; }
    public NativeList<VkQueueFamilyProperties> QueueFamilyProperties { get; } = new NativeList<VkQueueFamilyProperties>();
    public QueueFamilyIndices QFIndices;
    public VkCommandPool CommandPool { get; set; }

    public VkPhysicalDeviceFeatures enabledFeatures { get; set; }
    public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

    public void Initialize()
    {
        Instance = CreateInstance();
        PhysicalDevice = GetPhysicalDevice();
        Device = CreateLogicalDevice(enabledFeatures, EnabledExtensions);

    }

    public void Cleanup()
    {
        vkDestroyInstance(Instance, nint.Zero);
    }

    public void RenderTriangle()
    {
    }

    private unsafe VkDevice CreateLogicalDevice(VkPhysicalDeviceFeatures enabledFeatures, NativeList<IntPtr> enabledExtensions, bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute)
    {
        uint queueFamilyCount = 0;
        vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, ref queueFamilyCount, null);
        Debug.Assert(queueFamilyCount > 0);
        QueueFamilyProperties.Resize(queueFamilyCount);
        vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueFamilyCount, (VkQueueFamilyProperties*)QueueFamilyProperties.Data.ToPointer());
        QueueFamilyProperties.Count = queueFamilyCount;

        // Get list of supported extensions
        uint extCount = 0;
        vkEnumerateDeviceExtensionProperties(PhysicalDevice, (byte*)null, ref extCount, null);
        if (extCount > 0)
        {
            VkExtensionProperties* extensions = stackalloc VkExtensionProperties[(int)extCount];
            if (vkEnumerateDeviceExtensionProperties(PhysicalDevice, (byte*)null, ref extCount, extensions) == VkResult.Success)
            {
                for (uint i = 0; i < extCount; i++)
                {
                    var ext = extensions[i];
                    // supportedExtensions.push_back(ext.extensionName);
                    // TODO: fixed-length char arrays are not being parsed correctly.
                }
            }
        }

        // Desired queues need to be requested upon logical device creation
        // Due to differing queue family configurations of Vulkan implementations this can be a bit tricky, especially if the application
        // requests different queue types

        using (NativeList<VkDeviceQueueCreateInfo> queueCreateInfos = new NativeList<VkDeviceQueueCreateInfo>())
        {
            float defaultQueuePriority = 0.0f;

            // Graphics queue
            if ((requestedQueueTypes & VkQueueFlags.Graphics) != 0)
            {
                QFIndices.Graphics = GetQueueFamilyIndex(VkQueueFlags.Graphics);
                VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo();
                queueInfo.sType = VkStructureType.DeviceQueueCreateInfo;
                queueInfo.queueFamilyIndex = QFIndices.Graphics;
                queueInfo.queueCount = 1;
                queueInfo.pQueuePriorities = &defaultQueuePriority;
                queueCreateInfos.Add(queueInfo);
            }
            else
            {
                QFIndices.Graphics = (uint)NullHandle;
            }

            // Dedicated compute queue
            if ((requestedQueueTypes & VkQueueFlags.Compute) != 0)
            {
                QFIndices.Compute = GetQueueFamilyIndex(VkQueueFlags.Compute);
                if (QFIndices.Compute != QFIndices.Graphics)
                {
                    // If compute family index differs, we need an additional queue create info for the compute queue
                    VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo();
                    queueInfo.sType = VkStructureType.DeviceQueueCreateInfo;
                    queueInfo.queueFamilyIndex = QFIndices.Compute;
                    queueInfo.queueCount = 1;
                    queueInfo.pQueuePriorities = &defaultQueuePriority;
                    queueCreateInfos.Add(queueInfo);
                }
            }
            else
            {
                // Else we use the same queue
                QFIndices.Compute = QFIndices.Graphics;
            }

            // Dedicated transfer queue
            if ((requestedQueueTypes & VkQueueFlags.Transfer) != 0)
            {
                QFIndices.Transfer = GetQueueFamilyIndex(VkQueueFlags.Transfer);
                if (QFIndices.Transfer != QFIndices.Graphics && QFIndices.Transfer != QFIndices.Compute)
                {
                    // If compute family index differs, we need an additional queue create info for the transfer queue
                    VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo();
                    queueInfo.sType = VkStructureType.DeviceQueueCreateInfo;
                    queueInfo.queueFamilyIndex = QFIndices.Transfer;
                    queueInfo.queueCount = 1;
                    queueInfo.pQueuePriorities = &defaultQueuePriority;
                    queueCreateInfos.Add(queueInfo);
                }
            }
            else
            {
                // Else we use the same queue
                QFIndices.Transfer = QFIndices.Graphics;
            }

            // Create the logical device representation
            using (NativeList<IntPtr> deviceExtensions = new NativeList<IntPtr>(enabledExtensions))
            {
                if (useSwapChain)
                {
                    // If the device will be used for presenting to a display via a swapchain we need to request the swapchain extension
                    deviceExtensions.Add(Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);
                }

                VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();
                deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;
                deviceCreateInfo.pQueueCreateInfos = (VkDeviceQueueCreateInfo*)queueCreateInfos.Data.ToPointer();
                deviceCreateInfo.pEnabledFeatures = &enabledFeatures;

                if (deviceExtensions.Count > 0)
                {
                    deviceCreateInfo.enabledExtensionCount = deviceExtensions.Count;
                    deviceCreateInfo.ppEnabledExtensionNames = (byte**)deviceExtensions.Data.ToPointer();
                }

                VkResult result = vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out var device);
                if (result != VkResult.Success)
                {
                    throw new InvalidOperationException("Could not create Vulkan instance. Error: " + result);
                }

                CommandPool = CreateCommandPool(device, QFIndices.Graphics);

                return device;
            }
        }
    }

    private uint GetQueueFamilyIndex(VkQueueFlags queueFlags)
    {
        // Dedicated queue for compute
        // Try to find a queue family index that supports compute but not graphics
        if ((queueFlags & VkQueueFlags.Compute) != 0)
        {
            for (uint i = 0; i < QueueFamilyProperties.Count; i++)
            {
                if (((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
                    && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Graphics) == 0)
                {
                    return i;
                }
            }
        }

        // Dedicated queue for transfer
        // Try to find a queue family index that supports transfer but not graphics and compute
        if ((queueFlags & VkQueueFlags.Transfer) != 0)
        {
            for (uint i = 0; i < QueueFamilyProperties.Count; i++)
            {
                if (((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
                    && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Graphics) == 0
                    && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Compute) == 0)
                {
                    return i;
                }
            }
        }

        // For other queue types or if no separate compute queue is present, return the first one to support the requested flags
        for (uint i = 0; i < QueueFamilyProperties.Count; i++)
        {
            if ((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Could not find a matching queue family index");
    }

    private unsafe VkCommandPool CreateCommandPool(VkDevice device, uint queueFamilyIndex, VkCommandPoolCreateFlags createFlags = VkCommandPoolCreateFlags.ResetCommandBuffer)
    {
        VkCommandPoolCreateInfo cmdPoolInfo = VkCommandPoolCreateInfo.New();
        cmdPoolInfo.queueFamilyIndex = queueFamilyIndex;
        cmdPoolInfo.flags = createFlags;
        var result = vkCreateCommandPool(device, &cmdPoolInfo, null, out VkCommandPool cmdPool);
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException("Could not create Vulkan instance. Error: " + result);
        }
        return cmdPool;
    }

    private unsafe VkPhysicalDevice GetPhysicalDevice()
    {
        var gpuCount = 0u;
        vkEnumeratePhysicalDevices(Instance, &gpuCount, null);
        if (gpuCount == 0)
        {
            throw new InvalidOperationException("No GPU with Vulkan support found.");
        }

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)gpuCount];
        vkEnumeratePhysicalDevices(Instance, &gpuCount, physicalDevices);

        for (int i = 0; i < gpuCount; i++)
        {
            VkPhysicalDeviceProperties dp;
            vkGetPhysicalDeviceProperties(physicalDevices[i], &dp);
        }

        // GPU selection

        // Select physical Device to be used for the Vulkan example
        // Defaults to the first Device unless specified by command line

        uint selectedDevice = 0;
        // TODO: Implement arg parsing, etc.

        var physicalDevice = ((VkPhysicalDevice*)physicalDevices)[selectedDevice];

        // Store properties (including limits) and features of the phyiscal Device
        // So examples can check against them and see if a feature is actually supported
        VkPhysicalDeviceProperties deviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &deviceProperties);
        DeviceProperties = deviceProperties;

        VkPhysicalDeviceFeatures deviceFeatures;
        vkGetPhysicalDeviceFeatures(physicalDevice, &deviceFeatures);
        DeviceFeatures = deviceFeatures;

        // Gather physical Device memory properties
        VkPhysicalDeviceMemoryProperties deviceMemoryProperties;
        vkGetPhysicalDeviceMemoryProperties(physicalDevice, &deviceMemoryProperties);
        DeviceMemoryProperties = deviceMemoryProperties;

        return physicalDevice;

        // Derived examples can override this to set actual features (based on above readings) to enable for logical device creation
        //getEnabledFeatures();

        // Vulkan Device creation
        // This is handled by a separate class that gets a logical Device representation
        // and encapsulates functions related to a Device
        //vulkanDevice = new vksVulkanDevice(PhysicalDevice);
        //VkResult res = vulkanDevice.CreateLogicalDevice(enabledFeatures, EnabledExtensions);
        //if (res != VkResult.Success)
        //{
        //    throw new InvalidOperationException("Could not create Vulkan Device.");
        //}
        //device = vulkanDevice.LogicalDevice;

        //// Get a graphics queue from the Device
        //VkQueue queue;
        //vkGetDeviceQueue(device, vulkanDevice.QFIndices.Graphics, 0, &queue);
        //this.queue = queue;

        //// Find a suitable depth format
        //VkFormat depthFormat;
        //uint validDepthFormat = Tools.getSupportedDepthFormat(physicalDevice, &depthFormat);
        //Debug.Assert(validDepthFormat == True);
        //DepthFormat = depthFormat;
    }

    private unsafe VkInstance CreateInstance(bool enableValidation = false)
    {
        VkApplicationInfo appInfo = new()
        {
            sType = VkStructureType.ApplicationInfo,
            pApplicationName = new FixedUtf8String("VKEngine"),
            applicationVersion = VkVersion.Make(1, 0, 0),
            pEngineName = new FixedUtf8String("VKEngine"),
            engineVersion = VkVersion.Make(1, 0, 0),
            apiVersion = VkVersion.Make(1, 0, 0)
        };

        NativeList<IntPtr> instanceExtensions = new(2)
        {
            Strings.VK_KHR_SURFACE_EXTENSION_NAME
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            instanceExtensions.Add(Strings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            instanceExtensions.Add(Strings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        VkInstanceCreateInfo instanceCreateInfo = VkInstanceCreateInfo.New();
        instanceCreateInfo.pApplicationInfo = &appInfo;

        if (instanceExtensions.Count > 0)
        {
            if (enableValidation)
            {
                instanceExtensions.Add(Strings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
            }
            instanceCreateInfo.enabledExtensionCount = instanceExtensions.Count;
            instanceCreateInfo.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;
        }


        if (enableValidation)
        {
            NativeList<IntPtr> enabledLayerNames = new(1)
            {
                Strings.StandardValidationLayeName
            };
            instanceCreateInfo.enabledLayerCount = enabledLayerNames.Count;
            instanceCreateInfo.ppEnabledLayerNames = (byte**)enabledLayerNames.Data;
        }

        VkInstance instance;
        VkResult result = vkCreateInstance(&instanceCreateInfo, null, out instance);
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException("Could not create Vulkan instance. Error: " + result);
        }
        return instance;
    }
}
