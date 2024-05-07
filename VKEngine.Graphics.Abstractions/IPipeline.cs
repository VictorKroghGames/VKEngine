using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;

public readonly struct PipelineSpecification
{
    public FrontFace FrontFace { get; init; }

    public IShader Shader { get; init; }
    public IRenderPass RenderPass { get; init; }
}

public interface IPipelineFactory
{
    IPipeline CreateGraphicsPipeline(PipelineSpecification specification);
}

public interface IPipeline
{
    void Cleanup();

    void Bind();
    void Unbind();
}
