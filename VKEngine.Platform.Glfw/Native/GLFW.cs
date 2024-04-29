namespace VKEngine.Platform.Glfw.Native;

internal partial class GLFW
{
    public static bool Init() => Native.glfwInit();
    public static void Terminate() => Native.glfwTerminate();

    public static IntPtr CreateWindow(int width, int height, string title) => CreateWindow(width, height, title, IntPtr.Zero, IntPtr.Zero);
    public static IntPtr CreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share) => Native.glfwCreateWindow(width, height, title, monitor, share);

    public static void MakeContextCurrent(IntPtr window) => Native.glfwMakeContextCurrent(window);

    public static bool WindowShouldClose(IntPtr window) => Native.glfwWindowShouldClose(window);

    public static void SwapBuffers(IntPtr window) => Native.glfwSwapBuffers(window);

    public static void PollEvents() => Native.glfwPollEvents();

    public static int GetKey(IntPtr window, int key) => Native.glfwGetKey(window, key);
}
