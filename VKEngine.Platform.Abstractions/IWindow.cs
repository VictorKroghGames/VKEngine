namespace VKEngine.Platform;

public interface IWindow : IDisposable
{
    bool IsRunning { get; }

    void Initialize();
    void Update();
}
