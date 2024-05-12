using VKEngine.Graphics.Enumerations;

namespace VKEngine.Graphics;

public enum VertexInputRate
{
    Vertex,
    Instance
}

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

public readonly struct VertexLayoutElementDescription(string name, Format format)
{
    public readonly string Name => name;
    public readonly Format Format => format;
}

public readonly struct VertexLayoutDescription(uint binding = 0, params VertexLayoutElementDescription[] elements)
{
    public readonly uint Binding => binding;
    public readonly VertexLayoutElementDescription[] Elements => elements;
}

public readonly struct PipelineDescription
{
    public VertexLayoutDescription VertexLayout { get; init; }
    public IShader Shader { get; init; }
}

[Obsolete("Use 'PipelineDescription' instead!")]
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
    IPipeline CreateGraphicsPipeline(PipelineDescription description);
    IPipeline CreateGraphicsPipeline(PipelineSpecification specification, params IDescriptorSet[] descriptorSets);
}

public interface IPipeline
{
    void Cleanup();

    void Bind();
    void Unbind();
}
