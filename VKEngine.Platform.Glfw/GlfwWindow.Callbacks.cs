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

        internal static void OnCharEvent(IntPtr window, uint codepoint)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new KeyTypedEvent((int)codepoint));
        }

        // --------------------------------------------------------------------------------------------------
        // Mouse/Cursor callbacks
        // --------------------------------------------------------------------------------------------------

        internal static void OnMouseButtonEvent(IntPtr window, int button, int action, int mods)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            switch (action)
            {
                case GLFW.GLFW_PRESS:
                    data.callbackFunction?.Invoke(new MouseButtonPressedEvent(button));
                    break;
                case GLFW.GLFW_RELEASE:
                    data.callbackFunction?.Invoke(new MouseButtonReleasedEvent(button));
                    break;
            }
        }

        internal static void OnMousePositionEvent(IntPtr window, double x, double y)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new MousePositionEvent((float)x, (float)y));
        }

        internal static void OnMouseScrollEvent(IntPtr window, double xoffset, double yoffset)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new MouseScrollEvent((float)xoffset, (float)yoffset));
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

        internal static void OnWindowIconify(IntPtr window, int iconified)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new WindowMinimizeEvent(iconified == GLFW.GLFW_TRUE));
        }

        internal static void OnWindowMaximize(IntPtr window, int maximized)
        {
            var data = GLFW.GetWindowUserPointer<WindowData>(window);

            data.callbackFunction?.Invoke(new WindowMaximizeEvent(maximized == GLFW.GLFW_TRUE));
        }
    }
}
