namespace VKEngine.Graphics;

public interface IImageFactory
{
    IImage CreateImageFromFile(string filepath);
    IImage CreateImageFromMemory(int width, int height, IntPtr data, uint size);
}

public interface IImage
{
    void Cleanup();
}