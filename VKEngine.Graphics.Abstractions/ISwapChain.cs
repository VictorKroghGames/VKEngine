namespace VKEngine.Graphics;

public interface ISwapChain
{
    uint CurrentImageIndex { get; }

    ICommandBuffer CurrentCommandBuffer { get; }

    void AquireNextImage();
    void Present();
}
