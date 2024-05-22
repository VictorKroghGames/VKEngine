using VKEngine.Configuration;

namespace VKEngine.Platform;

public interface IPlatformManager
{
    IWindow CreateWindow(IPlatformConfiguration platformConfiguration);
    IInput CreateInput();

    void SetEventCallback(Action<IEvent> callback);
}

internal class PlatformManager(IWindowFactory windowFactory) : IPlatformManager
{
    public IWindow CreateWindow(IPlatformConfiguration platformConfiguration)
    {
        return windowFactory.CreateWindow(platformConfiguration);
    }

    public IInput CreateInput()
    {
        throw new NotImplementedException();
    }

    public void SetEventCallback(Action<IEvent> callback)
    {
        throw new NotImplementedException();
    }
}
