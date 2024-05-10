namespace VKEngine.Graphics;

public interface ITextureFactory
{
    ITexture CreateTextureFromFilePath(string filepath);
    ITexture CreateTextureFromImage(IImage image, bool disposeImage = true);
}

public interface ITexture
{
    void Cleanup();
}
