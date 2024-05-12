namespace VKEngine.Graphics;

public readonly struct PipelineDescription
{
    // LAYOUT
    public readonly IShader Shader { get; init; }
    public readonly VertexLayout[] VertexLayouts { get; init; }

    // INPUT ASSEMBLY
    public readonly PrimitiveTopology PrimitiveTopology { get; init; }
    public readonly bool PrimitiveRestartEnable { get; init; }

    // RASTERIZATION
    public readonly CullMode CullMode { get; init; }
    public readonly FrontFace FrontFace { get; init; }

    // MULTISAMPLING

    // COLOR BLENDING

    // DYNAMIC STATE

    // PIPELINE LAYOUT
    public readonly IDescriptorSet[] DescriptorSets { get; init; }

    // RENDER PASS
    public readonly IRenderPass RenderPass { get; init; }
}
