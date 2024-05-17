namespace VKEngine.Graphics;

public interface IDescriptorSetFactory
{
    IDescriptorSet CreateDescriptorSet(DescriptorSetDescription descriptorSetDescription);
    IDescriptorSet CreateDescriptorSet(uint maxSets, DescriptorSetDescription descriptorSetDescription);
}

public interface IDescriptorSet
{
    void Cleanup();

    void Update<T>(uint binding, uint arrayElement, IBuffer buffer, ulong offset = 0);
    void Update(uint binding, uint arrayElement, IBuffer buffer, ulong size, ulong offset = 0);
    void Update(uint binding, uint arrayElement, ITexture texture);
}