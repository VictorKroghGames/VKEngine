namespace VKEngine.Graphics;

public interface IShader
{
    string Name { get; }

    void Cleanup();
}
