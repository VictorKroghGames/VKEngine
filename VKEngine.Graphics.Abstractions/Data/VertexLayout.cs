namespace VKEngine.Graphics;

public readonly struct VertexLayoutAttribute(string name, Format format)
{
    public string Name { get; } = name;
    public Format Format { get; } = format;
}

public readonly struct VertexLayout(uint binding, VertexInputRate inputRate, params VertexLayoutAttribute[] attributes)
{
    public VertexLayout(params VertexLayoutAttribute[] attributes)
        : this(VertexInputRate.Vertex, attributes)
    {
    }

    public VertexLayout(VertexInputRate inputRate, params VertexLayoutAttribute[] attributes)
        : this(0, inputRate, attributes)
    {
    }

    public VertexLayout(uint binding, params VertexLayoutAttribute[] attributes)
        : this(binding, VertexInputRate.Vertex, attributes)
    {
    }

    public readonly uint Binding { get; } = binding;
    public readonly VertexInputRate InputRate { get; } = inputRate;
    public readonly VertexLayoutAttribute[] Attributes { get; } = attributes;
}
