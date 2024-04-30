using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed class GlfwWindow : IWindow
{
    internal IntPtr windowHandle;

    public bool IsRunning => GLFW.WindowShouldClose(windowHandle) is false;

    public int Width => 800;
    public int Height => 600;

    public nint NativeWindowHandle => windowHandle;

    public void Initialize()
    {
        if (GLFW.Init() is false)
        {
            Console.WriteLine("Failed to initialize GLFW.");
            return;
        }

        GLFW.WindowHint(GLFW.GLFW_CLIENT_API, GLFW.GLFW_NO_API);
        GLFW.WindowHint(GLFW.GLFW_RESIZABLE, GLFW.GLFW_FALSE);

        windowHandle = GLFW.CreateWindow(800, 600, "Hello, World!", IntPtr.Zero, IntPtr.Zero);
        if (windowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create window.");
            GLFW.Terminate();
            return;
        }

        GLFW.MakeContextCurrent(windowHandle);
    }

    public void Shutdown()
    {
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

    //public IEnumerable<string> GetRequiredInstanceExtensions()
    //{
    //    var extensionsPtr = GLFW.GetRequiredInstanceExtensions(out var count);
    //    if (extensionsPtr == IntPtr.Zero)
    //    {
    //        return Enumerable.Empty<string>();
    //    }

    //    if (count == 0)
    //    {
    //        return Enumerable.Empty<string>();
    //    }

    //    var extensions = new string[count];
    //    var offset = 0;
    //    for (var i = 0; i < count; i++, offset += IntPtr.Size)
    //    {
    //        var p = Marshal.ReadIntPtr(extensionsPtr, offset);
    //        var extension = Marshal.PtrToStringAnsi(p);
    //        if(string.IsNullOrWhiteSpace(extension))
    //        {
    //            continue;
    //        }
    //        extensions[i] = extension;
    //    }

    //    return extensions;
    //}
}
