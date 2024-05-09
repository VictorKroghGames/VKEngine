namespace VKEngine.Graphics;

public interface ISwapChain
{
    uint CurrentFrameIndex { get; }

    void Initialize(IRenderPass renderPass);
    void Cleanup();

    void AquireNextImage();
    void Present();
}
