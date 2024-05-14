using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed partial class GlfwWindow
{
    internal void OnWindowClose(IntPtr window)
    {
        data.callbackFunction?.Invoke(new WindowCloseEvent());
    }

    private void OnKeyEvent(IntPtr window, int key, int scancode, int action, int mods)
    {
        switch (action)
        {
            case GLFW.GLFW_PRESS:
                data.callbackFunction?.Invoke(new KeyPressedEvent(key));
                break;
            case GLFW.GLFW_RELEASE:
                data.callbackFunction?.Invoke(new KeyReleasedEvent(key));
                break;
            case GLFW.GLFW_REPEAT:
                data.callbackFunction?.Invoke(new KeyTypedEvent(key));
                break;
        }
    }
}
