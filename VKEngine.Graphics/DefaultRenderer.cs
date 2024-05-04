namespace VKEngine.Graphics;

internal class DefaultRenderer(IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary, ISwapChain swapChain) : IRenderer
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
        graphicsContext.Dispose();
    }

    public void BeginFrame()
    {
        swapChain.CurrentCommandBuffer.Begin();
    }

    public void EndFrame()
    {
        swapChain.CurrentCommandBuffer.End();
    }
}
