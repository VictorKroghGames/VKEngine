namespace VKEngine.Graphics;

public interface IRenderPassFactory
{
    IRenderPass CreateRenderPass();
}

public interface IRenderPass
{
    void Cleanup();

    void Bind();
    void Unbind();
}
