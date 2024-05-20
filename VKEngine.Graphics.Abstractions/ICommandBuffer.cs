namespace VKEngine.Graphics;

public interface ICommandBuffer
{
    void Cleanup();

    void Begin(CommandBufferUsageFlags flags = CommandBufferUsageFlags.None);
    void End();
    void Submit();

    void BeginRenderPass(IRenderPass renderPass);
    void EndRenderPass();

    void BindPipeline(IPipeline pipeline);
    void BindVertexBuffer(IBuffer buffer);
    void BindIndexBuffer(IBuffer buffer);
    void BindDescriptorSet(IPipeline pipeline, IDescriptorSet descriptorSet, uint set = 0);

    void Draw();
    void DrawIndex(uint indexCount);
}
