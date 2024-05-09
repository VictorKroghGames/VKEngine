namespace VKEngine.Graphics;

public interface IDescriptorSetFactory
{
    IDescriptorSet CreateDescriptorSet<T>(IBuffer buffer);
    IDescriptorSet CreateDescriptorSet<T>(uint maxSets, IBuffer buffer);
}

public interface IDescriptorSet
{
    void Cleanup();
}