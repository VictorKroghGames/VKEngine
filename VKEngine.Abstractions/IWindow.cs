namespace VKEngine;

public interface IWindow : IDisposable
{
    bool IsRunning { get; }

    int Width { get; }
    int Height { get; }

    IntPtr NativeWindowHandle { get; }

    void Initialize();
    void Shutdown();
    void Update();
}
