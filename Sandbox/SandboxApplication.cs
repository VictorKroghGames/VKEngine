using VKEngine.Core;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window) : IApplication
{
    public void Dispose()
    {
    }

    public void Run()
    {
        window.Initialize();

        while (window.IsRunning)
        {
            window.Update();
        }
    }
}
