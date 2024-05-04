namespace VKEngine.Graphics;

public interface IRenderer : IDisposable
{
    void Initialize();

    void BeginFrame();
    void EndFrame();
}
