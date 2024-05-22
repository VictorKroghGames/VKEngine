using VKEngine.Configuration;

namespace VKEngine.Platform;

public interface IWindowFactory
{
    IWindow CreateWindow(IPlatformConfiguration platformConfiguration);
}
