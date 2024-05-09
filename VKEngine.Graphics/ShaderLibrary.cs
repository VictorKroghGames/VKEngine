using System.Collections.Concurrent;

namespace VKEngine.Graphics;

internal sealed class ShaderLibrary(IShaderFactory shaderFactory) : IShaderLibrary
{
    private readonly IDictionary<string, IShader> shaders = new ConcurrentDictionary<string, IShader>();

    public void Cleanup()
    {
        foreach (var shader in shaders.Values)
        {
            shader.Cleanup();
        }

        shaders.Clear();
    }

    public IShader? Get(string name) => Get<IShader>(name);

    public TShader? Get<TShader>(string name) where TShader : IShader
    {
        if (shaders.TryGetValue(name, out var shader) is false)
        {
            return default;
        }

        if (shader is not TShader shaderOfType)
        {
            return default;
        }

        return shaderOfType;
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
