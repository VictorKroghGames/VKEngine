namespace VKEngine.Graphics;

public interface IRenderPassFactory
{
    IRenderPass CreateRenderPass(Format surfaceFormat);
}

public interface IRenderPass
{
    void Cleanup();

    void Bind();
    void Unbind();
}
