namespace VKEngine;

public delegate void EventCallbackFunction(IEvent e);

public interface IWindow : IDisposable
{
    bool IsRunning { get; }

    int Width { get; }
    int Height { get; }

    IntPtr NativeWindowHandle { get; }

    void Initialize();
    void Shutdown();

    void SetEventCallback(EventCallbackFunction eventCallback);

    void Update();
}
