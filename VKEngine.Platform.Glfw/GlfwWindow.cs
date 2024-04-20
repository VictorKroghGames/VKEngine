using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed class GlfwWindow : IWindow
{
    private IntPtr windowHandle;

    public bool IsRunning => GLFW.WindowShouldClose(windowHandle) is false;

    public void Initialize()
    {
        if (GLFW.Init() is false)
        {
            Console.WriteLine("Failed to initialize GLFW.");
            return;
        }

        windowHandle = GLFW.CreateWindow(800, 600, "Hello, World!", IntPtr.Zero, IntPtr.Zero);
        if (windowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create window.");
            GLFW.Terminate();
            return;
        }

        GLFW.MakeContextCurrent(windowHandle);
    }

    public void Dispose()
    {
        // Destroy window

        GLFW.Terminate();
    }

    public void Update()
    {
        GLFW.SwapBuffers(windowHandle);

        GLFW.PollEvents();
    }
}
