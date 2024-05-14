using VKEngine.Configuration;
using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed class WindowData
{
    internal EventCallbackFunction? callbackFunction;
}

internal sealed partial class GlfwWindow(IVKEngineConfiguration engineConfiguration, IPlatformConfiguration platformConfiguration) : IWindow
{
    internal GlfwNativeWindowHandle windowHandle = GlfwNativeWindowHandle.Null;
    private readonly WindowData data = new();

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

        GLFW.SetWindowCloseCallback(ref windowHandle, OnWindowClose);

        GLFW.SetKeyCallback(ref windowHandle, OnKeyEvent);
    }

    public void Shutdown()
    {
    }

    public void SetEventCallback(EventCallbackFunction eventCallback)
    {
        data.callbackFunction = eventCallback;
    }

    public void Dispose()
    {
        Shutdown();
        // Destroy window

        GLFW.Terminate();
    }

    public void Update()
    {
        //GLFW.SwapBuffers(windowHandle);

        GLFW.PollEvents();
    }
}
