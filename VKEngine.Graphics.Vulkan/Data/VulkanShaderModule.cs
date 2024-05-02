using VKEngine.Graphics.Vulkan.Native;
using Vulkan;

namespace VKEngine.Graphics.Vulkan.Data;

internal readonly struct VulkanShaderModule
{
    public ShaderModuleType Type { get; init; }
    public FixedUtf8String MainFunctionIdentifier { get; init; }
    public VkShaderModule Module { get; init; }

    internal VkShaderStageFlags ShaderStageFlags { get; init; }
}
