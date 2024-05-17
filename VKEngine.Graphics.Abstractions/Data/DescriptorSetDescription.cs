namespace VKEngine.Graphics;

public readonly struct DescriptorBinding
{
    public readonly uint Binding { get; init; }
    public readonly DescriptorType DescriptorType { get; init; }
    public readonly uint DescriptorCount { get; init; }
    public readonly ShaderModuleType ShaderStageFlags { get; init; }
}

public readonly struct DescriptorSetDescription
{
    public readonly DescriptorBinding[] DescriptorBindings { get; init; }
}
