namespace VKEngine.Graphics;

public interface ITextureFactory
{
    ITexture CreateFromImage(IImage image);
}

public interface ITexture
{
    void Cleanup();
}
