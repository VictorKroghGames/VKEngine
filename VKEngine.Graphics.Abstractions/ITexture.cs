namespace VKEngine.Graphics;

public interface ITextureFactory
{
    ITexture CreateTextureFromFilePath(string filepath);
    ITexture CreateTextureFromImage(IImage image);
}

public interface ITexture
{
    void Cleanup();
}
