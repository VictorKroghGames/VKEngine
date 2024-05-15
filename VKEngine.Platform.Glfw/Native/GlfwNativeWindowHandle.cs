namespace VKEngine.Platform.Glfw.Native;

public struct GlfwNativeWindowHandle
{
    public static GlfwNativeWindowHandle Null => new GlfwNativeWindowHandle { WindowHandle = IntPtr.Zero };

    public IntPtr WindowHandle { get; set; }

    internal GLFW.Callbacks.GLFWerrorfun GlfwErrorFunc { get; set; }

    #region Window callbacks
    internal GLFW.Callbacks.GLFWwindowposfun WindowPosEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowsizefun WindowSizeEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowclosefun WindowCloseEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowrefreshfun WindowRefreshEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowfocusfun WindowFocusEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowiconifyfun WindowIconifyEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWwindowmaximizefun WindowMaximizeEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWframebuffersizefun FramebufferSizeEventFunc { get; set; }
    #endregion

    #region Key callbacks
    internal GLFW.Callbacks.GLFWkeyfun KeyEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWcharfun CharEventFunc { get; set; }
    #endregion

    #region Mouse callbacks
    internal GLFW.Callbacks.GLFWmousebuttonfun MouseButtonEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWcursorposfun CursorPosEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWcursorenterfun CursorEnterEventFunc { get; set; }
    internal GLFW.Callbacks.GLFWscrollfun ScrollEventFunc { get; set; }
    #endregion

    public static implicit operator IntPtr(GlfwNativeWindowHandle windowHandle) => windowHandle.WindowHandle;
    public static implicit operator GlfwNativeWindowHandle(IntPtr windowHandle) => new() { WindowHandle = windowHandle };
}
