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

    public static bool Init() => Native.glfwInit();
    public static void Terminate() => Native.glfwTerminate();

    public static GlfwNativeWindowHandle CreateWindow(int width, int height, string title) => CreateWindow(width, height, title, IntPtr.Zero, IntPtr.Zero);
    public static GlfwNativeWindowHandle CreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share) => Native.glfwCreateWindow(width, height, title, monitor, share);

    public static void MakeContextCurrent(GlfwNativeWindowHandle windowHandle) => Native.glfwMakeContextCurrent(windowHandle);

    public static bool WindowShouldClose(GlfwNativeWindowHandle windowHandle) => Native.glfwWindowShouldClose(windowHandle);

    public static void SwapBuffers(GlfwNativeWindowHandle windowHandle) => Native.glfwSwapBuffers(windowHandle);

    public static void PollEvents() => Native.glfwPollEvents();

    internal static void WindowHint(int hint, int value) => Native.glfwWindowHint(hint, value);

    public static int GetKey(GlfwNativeWindowHandle windowHandle, int key) => Native.glfwGetKey(windowHandle, key);

    public static void SetWindowCloseCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWwindowclosefun cbfun)
    {
        windowHandle.WindowCloseEventFunc = cbfun;
        Native.glfwSetWindowCloseCallback(windowHandle, windowHandle.WindowCloseEventFunc);
    }

    public static void SetKeyCallback(ref GlfwNativeWindowHandle windowHandle, Callbacks.GLFWkeyfun keyfun)
    {
        windowHandle.KeyEventFunc = keyfun;
        Native.glfwSetKeyCallback(windowHandle, windowHandle.KeyEventFunc);
    }
}
