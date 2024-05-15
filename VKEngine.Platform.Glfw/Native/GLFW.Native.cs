using System.Runtime.InteropServices;

namespace VKEngine.Platform.Glfw.Native;

internal partial class GLFW
{
    internal static partial class Native
    {
        public const string LibraryName = "libs/glfw3.dll";

        [LibraryImport(LibraryName, EntryPoint = "glfwInit")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool glfwInit();

        [LibraryImport(LibraryName, EntryPoint = "glfwTerminate")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwTerminate();

        [LibraryImport(LibraryName, EntryPoint = "glfwCreateWindow")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial IntPtr glfwCreateWindow(
            [MarshalAs(UnmanagedType.I4)] int width,
            [MarshalAs(UnmanagedType.I4)] int height,
            [MarshalAs(UnmanagedType.LPStr)] string title,
            IntPtr monitor,
            IntPtr share);

        [LibraryImport(LibraryName, EntryPoint = "glfwMakeContextCurrent")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwMakeContextCurrent(
            IntPtr window);

        [LibraryImport(LibraryName, EntryPoint = "glfwWindowShouldClose")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool glfwWindowShouldClose(
            IntPtr window);

        [LibraryImport(LibraryName, EntryPoint = "glfwSwapBuffers")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwSwapBuffers(IntPtr window);

        [LibraryImport(LibraryName, EntryPoint = "glfwPollEvents")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwPollEvents();

        [LibraryImport(LibraryName, EntryPoint = "glfwWindowHint")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwWindowHint([MarshalAs(UnmanagedType.I4)] int hint, [MarshalAs(UnmanagedType.I4)] int value);

        [LibraryImport(LibraryName, EntryPoint = "glfwGetKey")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static partial int glfwGetKey(IntPtr window, [MarshalAs(UnmanagedType.I4)] int key);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowUserPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial void glfwSetWindowUserPointer(IntPtr window, IntPtr pointer);

        [LibraryImport(LibraryName, EntryPoint = "glfwGetWindowUserPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial IntPtr glfwGetWindowUserPointer(IntPtr window);

        // Window callbacks

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowPosCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowposfun glfwSetWindowPosCallback(IntPtr window, Callbacks.GLFWwindowposfun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowSizeCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowsizefun glfwSetWindowSizeCallback(IntPtr window, Callbacks.GLFWwindowsizefun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowCloseCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowclosefun glfwSetWindowCloseCallback(IntPtr window, Callbacks.GLFWwindowclosefun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowRefreshCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowrefreshfun glfwSetWindowRefreshCallback(IntPtr window, Callbacks.GLFWwindowrefreshfun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowFocusCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowfocusfun glfwSetWindowFocusCallback(IntPtr window, Callbacks.GLFWwindowfocusfun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetWindowIconifyCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWwindowiconifyfun glfwSetWindowIconifyCallback(IntPtr window, Callbacks.GLFWwindowiconifyfun cbfun);

        [LibraryImport(LibraryName, EntryPoint = "glfwSetFramebufferSizeCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWframebuffersizefun glfwSetFramebufferSizeCallback(IntPtr window, Callbacks.GLFWframebuffersizefun cbfun);

        // Input callbacks

        [LibraryImport(LibraryName, EntryPoint = "glfwSetKeyCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        internal static partial Callbacks.GLFWkeyfun glfwSetKeyCallback(IntPtr window, Callbacks.GLFWkeyfun cbfun);
    }
}
