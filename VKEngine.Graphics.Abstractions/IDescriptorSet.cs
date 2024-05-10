namespace VKEngine.Graphics;

public interface IDescriptorSetFactory
{
    IDescriptorSet CreateDescriptorSet<T>(IBuffer buffer, ITexture texture);
    IDescriptorSet CreateDescriptorSet<T>(uint maxSets, IBuffer buffer, ITexture texture);
}

public interface IDescriptorSet
{
    void Cleanup();
}