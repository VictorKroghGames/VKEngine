namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanShaderFactory(IVulkanLogicalDevice vulkanLogicalDevice) : IShaderFactory
{
    public IShader CreateShader(string? name, string vertexShaderFilePath, string fragmentShaderFilePath)
    {
        return CreateShader(name, new ShaderModuleSpecification(vertexShaderFilePath, ShaderModuleType.Vertex), new ShaderModuleSpecification(fragmentShaderFilePath, ShaderModuleType.Fragment));
    }

    public IShader CreateShader(string? name, params ShaderModuleSpecification[] shaderModuleSpecifications)
    {
        var shaderName = name ?? "Unknown Shader";
        var shader = new VulkanShader(vulkanLogicalDevice, shaderName, shaderModuleSpecifications);
        shader.Initialize();
        return shader;
    }
}
