using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanRenderer : IRenderer
{
    private VkInstance Instance { get; set; }
    public VkPhysicalDevice PhysicalDevice { get; set; }
    public VkDevice Device { get; set; }
    public VkPhysicalDeviceMemoryProperties DeviceMemoryProperties { get; set; }
    public VkPhysicalDeviceProperties DeviceProperties { get; set; }
    public VkPhysicalDeviceFeatures DeviceFeatures { get; set; }

    public void Initialize()
    {
        Instance = CreateInstance();
        PhysicalDevice = GetPhysicalDevice();
        Device = CreateLogicalDevice();
    }

    public void Cleanup()
    {
        vkDestroyInstance(Instance, nint.Zero);
    }

    public void RenderTriangle()
    {
    }

    private unsafe VkDevice CreateLogicalDevice()
    {


        return VkDevice.Null;
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
        vkGetPhysicalDeviceProperties(PhysicalDevice, &deviceProperties);
        DeviceProperties = deviceProperties;

        VkPhysicalDeviceFeatures deviceFeatures;
        vkGetPhysicalDeviceFeatures(PhysicalDevice, &deviceFeatures);
        DeviceFeatures = deviceFeatures;

        // Gather physical Device memory properties
        VkPhysicalDeviceMemoryProperties deviceMemoryProperties;
        vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, &deviceMemoryProperties);
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
