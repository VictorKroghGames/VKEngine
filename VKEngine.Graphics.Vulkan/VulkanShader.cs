using System.Net.Http.Headers;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal struct VulkanShaderModule
{
    public ShaderModuleType Type { get; init; }
    public FixedUtf8String MainFunctionIdentifier { get; init; }
    public VkShaderModule Module { get; init; }

    internal VkShaderStageFlags ShaderStageFlags { get; init; }
}

internal interface IVulkanShader : IShader
{
    IEnumerable<VulkanShaderModule> GetShaderModules();
}

internal sealed class VulkanShader(IVulkanLogicalDevice vulkanLogicalDevice, string name, params ShaderModuleSpecification[] shaderModuleSpecifications) : IVulkanShader
{
    private readonly IDictionary<ShaderModuleType, VulkanShaderModule> shaderModules = new Dictionary<ShaderModuleType, VulkanShaderModule>();

    private VkShaderModule vertexShaderModule = VkShaderModule.Null;
    private VkShaderModule fragmentShaderModule = VkShaderModule.Null;

    public string Name => name;

    internal void Initialize()
    {
        foreach (var shaderModuleSpecification in shaderModuleSpecifications)
        {
            if (shaderModules.ContainsKey(shaderModuleSpecification.Type) is true)
            {
                continue;
            }

            var shaderModule = CreateShader(shaderModuleSpecification.FilePath);
            shaderModules.TryAdd(shaderModuleSpecification.Type, new VulkanShaderModule
            {
                Type = shaderModuleSpecification.Type,
                MainFunctionIdentifier = new FixedUtf8String(shaderModuleSpecification.MainFunctionIdendifier),
                Module = shaderModule,
                ShaderStageFlags = shaderModuleSpecification.Type is ShaderModuleType.Vertex ? VkShaderStageFlags.Vertex : VkShaderStageFlags.Fragment
            });
        }
    }

    private VkShaderModule CreateShader(string filepath) => CreateShaderUnsafe(File.ReadAllBytes(filepath));

    private unsafe VkShaderModule CreateShaderUnsafe(byte[] bytecode)
    {
        VkShaderModuleCreateInfo shaderModuleCreateInfo = VkShaderModuleCreateInfo.New();
        fixed (byte* byteCodePtr = bytecode)
        {
            shaderModuleCreateInfo.pCode = (uint*)byteCodePtr;
            shaderModuleCreateInfo.codeSize = new UIntPtr((uint)bytecode.Length);
            var result = vkCreateShaderModule(vulkanLogicalDevice.Device, ref shaderModuleCreateInfo, null, out VkShaderModule module);
            if (result != VkResult.Success)
            {
                throw new ApplicationException("Failed to create shader module");
            }
            return module;
        }
    }

    public IEnumerable<VulkanShaderModule> GetShaderModules()
    {
        return shaderModules.Values;
    }
}
