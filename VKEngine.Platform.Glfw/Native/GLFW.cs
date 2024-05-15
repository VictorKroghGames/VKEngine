using System.Runtime.CompilerServices;

namespace VKEngine.Platform.Glfw.Native;

internal partial class GLFW
{
    internal const int GLFW_RESIZABLE = 0x00020003;
    internal const int GLFW_CLIENT_API = 0x00022001;
    internal const int GLFW_FALSE = 0;
    internal const int GLFW_NO_API = 0;
    internal const int GLFW_TRUE = 1;

    internal const int GLFW_PRESS = 1;
    internal const int GLFW_RELEASE = 0;
    internal const int GLFW_REPEAT = 2;

    internal static bool Init() => Native.glfwInit();
    internal static void Terminate() => Native.glfwTerminate();

    internal static GlfwNativeWindowHandle CreateWindow(int width, int height, string title) => CreateWindow(width, height, title, IntPtr.Zero, IntPtr.Zero);
    internal static GlfwNativeWindowHandle CreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share) => Native.glfwCreateWindow(width, height, title, monitor, share);

    internal static void MakeContextCurrent(GlfwNativeWindowHandle windowHandle) => Native.glfwMakeContextCurrent(windowHandle);

    internal static bool WindowShouldClose(GlfwNativeWindowHandle windowHandle) => Native.glfwWindowShouldClose(windowHandle);

    internal static void SwapBuffers(GlfwNativeWindowHandle windowHandle) => Native.glfwSwapBuffers(windowHandle);

    internal static void PollEvents() => Native.glfwPollEvents();

    internal static void WindowHint(int hint, int value) => Native.glfwWindowHint(hint, value);

    internal static int GetKey(GlfwNativeWindowHandle windowHandle, int key) => Native.glfwGetKey(windowHandle, key);

    internal unsafe static void SetWindowUserPointer<T>(GlfwNativeWindowHandle windowHandle, ref T data)
    {
        fixed (void* pData = &data)
        {
            SetWindowUserPointerNative(windowHandle, new nint(pData));
        }
    }

    internal static void SetWindowUserPointerNative(GlfwNativeWindowHandle windowHandle, IntPtr pointer) => Native.glfwSetWindowUserPointer(windowHandle, pointer);

    internal unsafe static T GetWindowUserPointer<T>(GlfwNativeWindowHandle windowHandle)
    {
        var userPointer = GetWindowUserPointerNative(windowHandle).ToPointer();
        return *((T*)userPointer);
    }

    internal static IntPtr GetWindowUserPointerNative(GlfwNativeWindowHandle windowHandle) => Native.glfwGetWindowUserPointer(windowHandle);

    // Window callbacks

    internal static void SetWindowPosCallback(GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowposfun func)
    {
        windowHandle.WindowPosEventFunc = func;
        Native.glfwSetWindowPosCallback(windowHandle, windowHandle.WindowPosEventFunc);
    }

    internal static void SetWindowSizeCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowsizefun func)
    {
        windowHandle.WindowSizeEventFunc = func;
        Native.glfwSetWindowSizeCallback(windowHandle, windowHandle.WindowSizeEventFunc);
    }

    internal static void SetWindowCloseCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowclosefun func)
    {
        windowHandle.WindowCloseEventFunc = func;
        Native.glfwSetWindowCloseCallback(windowHandle, windowHandle.WindowCloseEventFunc);
    }

    internal static void SetWindowRefreshCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowrefreshfun func)
    {
        windowHandle.WindowRefreshEventFunc = func;
        Native.glfwSetWindowRefreshCallback(windowHandle, windowHandle.WindowRefreshEventFunc);
    }

    internal static void SetWindowFocusCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowfocusfun func)
    {
        windowHandle.WindowFocusEventFunc = func;
        Native.glfwSetWindowFocusCallback(windowHandle, windowHandle.WindowFocusEventFunc);
    }

    internal static void SetWindowIconifyCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowiconifyfun func)
    {
        windowHandle.WindowIconifyEventFunc = func;
        Native.glfwSetWindowIconifyCallback(windowHandle, windowHandle.WindowIconifyEventFunc);
    }

    internal static void SetFramebufferSizeCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWframebuffersizefun func)
    {
        windowHandle.FramebufferSizeEventFunc = func;
        Native.glfwSetFramebufferSizeCallback(windowHandle, windowHandle.FramebufferSizeEventFunc);
    }

    // Key callbacks

    internal static void SetKeyCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWkeyfun keyfun)
    {
        windowHandle.KeyEventFunc = keyfun;
        Native.glfwSetKeyCallback(windowHandle, windowHandle.KeyEventFunc);
    }
}
