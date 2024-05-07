namespace VKEngine.Graphics;

public interface ICommandBuffer
{
    void Cleanup();

    void Begin();
    void End();
    void Submit();

    void BeginRenderPass(IRenderPass renderPass);
    void EndRenderPass();

    void BindPipeline(IPipeline pipeline);
    void BindBuffer(IVertexBuffer vertexBuffer);
    void Draw();
}

public interface ICommandBufferAllocator
{
    ICommandBuffer AllocateCommandBuffer();
    void FreeCommandBuffer(ICommandBuffer commandBuffer);
}
