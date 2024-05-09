namespace VKEngine.Graphics;

public interface IRenderer
{
    void Initialize();
    void Cleanup();

    void BeginFrame();
    void EndFrame();
    void Render();

    void Wait();

    void Draw(IRenderPass renderPass, IPipeline pipeline, IBuffer vertexBuffer, IBuffer indexBuffer, IDescriptorSet descriptorSet);
}
