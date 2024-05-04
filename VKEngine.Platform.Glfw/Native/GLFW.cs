namespace VKEngine.Platform.Glfw.Native;

internal partial class GLFW
{
    internal const int GLFW_RESIZABLE = 0x00020003;
    internal const int GLFW_CLIENT_API = 0x00022001;
    internal const int GLFW_FALSE = 0;
    internal const int GLFW_NO_API = 0;
    internal const int GLFW_TRUE = 1;

    public static bool Init() => Native.glfwInit();
    public static void Terminate() => Native.glfwTerminate();

    public static IntPtr CreateWindow(int width, int height, string title) => CreateWindow(width, height, title, IntPtr.Zero, IntPtr.Zero);
    public static IntPtr CreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share) => Native.glfwCreateWindow(width, height, title, monitor, share);

    public static void MakeContextCurrent(IntPtr window) => Native.glfwMakeContextCurrent(window);

    public static bool WindowShouldClose(IntPtr window) => Native.glfwWindowShouldClose(window);

    public static void SwapBuffers(IntPtr window) => Native.glfwSwapBuffers(window);

    public static void PollEvents() => Native.glfwPollEvents();

    internal static void WindowHint(int hint, int value) => Native.glfwWindowHint(hint, value);

    public static int GetKey(IntPtr window, int key) => Native.glfwGetKey(window, key);
}
