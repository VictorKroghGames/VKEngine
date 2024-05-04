﻿using VKEngine.Configuration;
using VKEngine.Platform.Glfw.Native;

namespace VKEngine.Platform.Glfw;

internal sealed class GlfwWindow(IVKEngineConfiguration engineConfiguration) : IWindow
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
}
