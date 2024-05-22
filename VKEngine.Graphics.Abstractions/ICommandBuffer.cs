using System.Numerics;

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

    void SetScissor(Vector4 scissor);

    void Draw();
    void DrawIndex(uint indexCount);
    void DrawIndexed(uint indexCount);
    void DrawIndexed(uint indexCount, uint firstIndex, int vertexOffset);
}
