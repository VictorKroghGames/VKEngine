namespace VKEngine.Graphics;

public interface ISwapChain
{
    void Initialize(IRenderPass renderPass);
    void Cleanup();
}
