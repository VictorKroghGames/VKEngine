using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VKEngine.Configuration;
using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal class WindowData
{
    [MarshalAs(UnmanagedType.FunctionPtr)]
    internal EventCallbackFunction? callbackFunction;
}

internal sealed partial class GlfwWindow(IVKEngineConfiguration engineConfiguration, IPlatformConfiguration platformConfiguration) : IWindow
{
    internal GlfwNativeWindowHandle windowHandle = GlfwNativeWindowHandle.Null;
    private WindowData data = new();

    public bool IsRunning => GLFW.WindowShouldClose(windowHandle) is false;

    public int Width => platformConfiguration.WindowWidth;
    public int Height => platformConfiguration.WindowHeight;

    public nint NativeWindowHandle => windowHandle.WindowHandle;

    public void Initialize()
    {
        if (GLFW.Init() is false)
        {
            Console.WriteLine("Failed to initialize GLFW.");
            return;
        }

        CreateWindow(engineConfiguration.PlatformConfiguration);
    }

    private void CreateWindow(IPlatformConfiguration platformConfiguration)
    {
        GLFW.WindowHint(GLFW.GLFW_CLIENT_API, GLFW.GLFW_NO_API);
        GLFW.WindowHint(GLFW.GLFW_RESIZABLE, platformConfiguration.IsResizable ? GLFW.GLFW_TRUE : GLFW.GLFW_FALSE);

        windowHandle = GLFW.CreateWindow(platformConfiguration.WindowWidth, platformConfiguration.WindowHeight, platformConfiguration.WindowTitle, IntPtr.Zero, IntPtr.Zero);
        if (windowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create window.");
            GLFW.Terminate();
            return;
        }

        GLFW.MakeContextCurrent(windowHandle);

        GLFW.SetWindowUserPointer(windowHandle, ref data);

        // Key events
        GLFW.SetKeyCallback(ref windowHandle, Callbacks.OnKeyEvent);
        GLFW.SetCharCallback(ref windowHandle, Callbacks.OnCharEvent);

        // Mouse events
        GLFW.SetMouseButtonCallback(ref windowHandle, Callbacks.OnMouseButtonEvent);
        GLFW.SetCursorPosCallback(ref windowHandle, Callbacks.OnMousePositionEvent);
        //GLFW.SetCursorEnterCallback(ref windowHandle, Callbacks.OnMouseEnterEvent);
        GLFW.SetScrollCallback(ref windowHandle, Callbacks.OnMouseScrollEvent);

        // Window events
        // GLFW.SetWindowPosCallback(ref windowHandle, Callbacks.OnWindowMove);
        GLFW.SetWindowSizeCallback(ref windowHandle, Callbacks.OnWindowResize);
        GLFW.SetWindowCloseCallback(ref windowHandle, Callbacks.OnWindowClose);
        //GLFW.SetWindowRefreshCallback(ref windowHandle, Callbacks.OnWindowRefresh);
        GLFW.SetWindowFocusCallback(ref windowHandle, Callbacks.OnWindowFocus);
        GLFW.SetWindowIconifyCallback(ref windowHandle, Callbacks.OnWindowIconify);
        GLFW.SetWindowMaximizeCallback(ref windowHandle, Callbacks.OnWindowMaximize);
        //GLFW.SetFramebufferSizeCallback(ref windowHandle, Callbacks.OnFramebufferResize);
        //GLFW.SetWindowContentScaleCallback(ref windowHandle, Callbacks.OnWindowContentScale);
    }

    public void SetEventCallback(EventCallbackFunction eventCallback)
    {
        data.callbackFunction = eventCallback;
    }

    public void Dispose()
    {
        GLFW.Terminate();
    }

    public void Update()
    {
        GLFW.PollEvents();
    }
}
