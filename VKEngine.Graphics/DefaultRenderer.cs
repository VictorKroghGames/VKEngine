namespace VKEngine.Graphics;

internal class DefaultRenderer(IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary) : IRenderer
{
    public void Initialize()
    {
        graphicsContext.Initialize();

        shaderLibrary.Load("shader",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv"), ShaderModuleType.Fragment)
        );
    }

    public void Dispose()
    {
        graphicsContext.Cleanup();
    }

    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
    }
}
