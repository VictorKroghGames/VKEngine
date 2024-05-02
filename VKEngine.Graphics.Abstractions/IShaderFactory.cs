namespace VKEngine.Graphics;

public interface IShaderFactory
{
    IShader CreateShader(string name, string vertexShaderFilePath, string fragmentShaderFilePath);
    IShader CreateShader(string name, params ShaderModuleSpecification[] shaderModuleSpecifications);
}
