using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed class GlfwInput(IWindow window) : IInput
{
    public bool IsKeyReleased(KeyCodes key)
    {
        var windowHandle = ((GlfwWindow)window).windowHandle;

        return GLFW.GetKey(windowHandle, (int)key) == 0;
    }

    public bool IsKeyPressed(KeyCodes key)
    {
        var windowHandle = ((GlfwWindow)window).windowHandle;

        return GLFW.GetKey(windowHandle, (int)key) == 1;
    }
}
