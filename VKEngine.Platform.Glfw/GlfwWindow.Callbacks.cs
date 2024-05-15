using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed partial class GlfwWindow
{
    internal class Callbacks
    {
        // --------------------------------------------------------------------------------------------------
        // Key callbacks
        // --------------------------------------------------------------------------------------------------

        internal static void OnKeyEvent(IntPtr window, int key, int scancode, int action, int mods)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            switch (action)
            {
                case GLFW.GLFW_PRESS:
                    data.callbackFunction?.Invoke(new KeyPressedEvent(key, false));
                    break;
                case GLFW.GLFW_RELEASE:
                    data.callbackFunction?.Invoke(new KeyReleasedEvent(key));
                    break;
                case GLFW.GLFW_REPEAT:
                    data.callbackFunction?.Invoke(new KeyPressedEvent(key, true));
                    break;
            }
        }

        // --------------------------------------------------------------------------------------------------
        // Windows callbacks
        // --------------------------------------------------------------------------------------------------

        internal static void OnWindowResize(IntPtr window, int width, int height)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new WindowResizeEvent(width, height));
        }

        internal static void OnWindowClose(IntPtr window)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new WindowCloseEvent());
        }

        internal static void OnWindowFocus(IntPtr window, int focused)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new WindowFocusEvent(focused == GLFW.GLFW_TRUE));
        }
    }
}
