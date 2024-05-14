namespace VKEngine.Platform.Glfw.Native;

public struct GlfwNativeWindowHandle
{
    public static GlfwNativeWindowHandle Null => new GlfwNativeWindowHandle { WindowHandle = IntPtr.Zero };

    public IntPtr WindowHandle { get; set; }

    internal GLFW.Callbacks.GLFWerrorfun GlfwErrorFunc { get; set; }

    #region Window callbacks
    internal GLFW.Callbacks.GLFWwindowclosefun WindowCloseEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowfocusfun WindowFocusEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowsizefun WindowResizeEventFunc { get; set; }
    #endregion

    #region Key callbacks
    internal GLFW.Callbacks.GLFWkeyfun KeyEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWcharfun KeyTypedFunc { get; set; }
    #endregion

    #region Mouse callbacks
    internal GLFW.Callbacks.GLFWscrollfun ScrollFunc { get; set; }
    internal GLFW.Callbacks.GLFWcursorposfun MouseMovedFunc { get; set; }
    internal GLFW.Callbacks.GLFWmousebuttonfun MouseButtonFunc { get; set; }
    #endregion

    public static implicit operator IntPtr(GlfwNativeWindowHandle windowHandle) => windowHandle.WindowHandle;
    public static implicit operator GlfwNativeWindowHandle(IntPtr windowHandle) => new() { WindowHandle = windowHandle };
}
