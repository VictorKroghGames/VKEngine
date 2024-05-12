using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;


public readonly struct PipelineLayoutVertexAttribute(uint binding, uint location, Format format, uint offset)
{
    public readonly uint binding = binding;
    public readonly uint location = location;
    public readonly Format format = format;
    public readonly uint offset = offset;
}

public readonly struct PipelineLayout(uint binding, uint stride, VertexInputRate inputRate, params PipelineLayoutVertexAttribute[] vertexAttributes)
{
    public readonly uint binding = binding;
    public readonly uint stride = stride;
    public readonly VertexInputRate inputRate = inputRate;
    public readonly PipelineLayoutVertexAttribute[] vertexAttributes = vertexAttributes;
}

public readonly struct PipelineSpecification
{
    public FrontFace FrontFace { get; init; }
    public CullMode CullMode { get; init; }
    public PipelineLayout PipelineLayout { get; init; }

    public IShader Shader { get; init; }
    public IRenderPass RenderPass { get; init; }
}

public interface IPipelineFactory
{
    IPipeline CreateGraphicsPipeline(PipelineDescription pipelineDescription);

    [Obsolete(error: true, message: "Use CreateGraphicsPipeline(PipelineDescription) instead.")]
    IPipeline CreateGraphicsPipeline(PipelineSpecification specification, params IDescriptorSet[] descriptorSets);
}

public interface IPipeline
{
    void Cleanup();

    void Bind();
    void Unbind();
}
