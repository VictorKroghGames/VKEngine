namespace VKEngine.Graphics;

public interface ITextureFactory
{
    ITexture CreateTextureFromFilePath(string filepath);
    ITexture CreateTextureFromImage(IImage image, bool disposeImage = true);
    ITexture CreateTextureFromMemory(int width, int height, IntPtr data, uint size);
}

public interface ITexture
{
    void Cleanup();
}
