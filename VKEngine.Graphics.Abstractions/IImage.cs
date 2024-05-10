namespace VKEngine.Graphics;

public interface IImageFactory
{
    IImage CreateImageFromFile(string filepath);
}

public interface IImage
{
    void Cleanup();
}