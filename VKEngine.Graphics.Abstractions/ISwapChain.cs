namespace VKEngine.Graphics;

public interface ISwapChain
{
    uint CurrentFrameIndex { get; }
    IRenderPass RenderPass { get; }

    void Initialize();
    void Cleanup();

    void AquireNextImage();
    void Present();
}
