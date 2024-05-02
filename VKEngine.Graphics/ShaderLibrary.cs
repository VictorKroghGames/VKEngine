using System.Collections.Concurrent;

namespace VKEngine.Graphics;

public interface IShaderLibrary
{
    IShader? Get(string name);
    void Load(string name, string vertexShaderFilePath, string fragmentShaderFilePath);
    void Load(string name, params ShaderModuleSpecification[] shaderModuleSpecifications);
}

internal sealed class ShaderLibrary(IShaderFactory shaderFactory) : IShaderLibrary
{
    private readonly IDictionary<string, IShader> shaders = new ConcurrentDictionary<string, IShader>();

    public IShader? Get(string name)
    {
        if (shaders.TryGetValue(name, out var shader) is false)
        {
            return default;
        }

        return shader;
    }

    public void Load(string name, string vertexShaderFilePath, string fragmentShaderFilePath)
    {
        var shader = shaderFactory.CreateShader(name, vertexShaderFilePath, fragmentShaderFilePath);
        shaders.TryAdd(name, shader);
    }

    public void Load(string name, params ShaderModuleSpecification[] shaderModuleSpecifications)
    {
        var shader = shaderFactory.CreateShader(name, shaderModuleSpecifications);
        shaders.TryAdd(name, shader);
    }
}
