using VKEngine.Configuration;

namespace VKEngine.Platform.Glfw;

internal sealed class GlfwWindowFactory : IWindowFactory
{
    public IWindow CreateWindow(IPlatformConfiguration platformConfiguration)
    {
        return new GlfwWindow(platformConfiguration);
    }
}
