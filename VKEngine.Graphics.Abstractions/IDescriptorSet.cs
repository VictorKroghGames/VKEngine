namespace VKEngine.Graphics;

public interface IDescriptorSetFactory
{
    IDescriptorSet CreateDescriptorSet<T>(IBuffer buffer);
}

public interface IDescriptorSet
{
    void Cleanup();
}