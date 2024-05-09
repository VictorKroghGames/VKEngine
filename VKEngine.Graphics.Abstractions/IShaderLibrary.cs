namespace VKEngine.Graphics;

public interface IShaderLibrary
{
    void Cleanup();

    IShader? Get(string name);
    TShader? Get<TShader>(string name) where TShader : IShader;
    void Load(string name, string vertexShaderFilePath, string fragmentShaderFilePath);
    void Load(string name, params ShaderModuleSpecification[] shaderModuleSpecifications);
}
