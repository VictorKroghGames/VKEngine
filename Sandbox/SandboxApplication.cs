using System.Collections.Concurrent;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IInput input, IRenderer renderer, IShaderLibrary shaderLibrary) : IApplication
{
    //[DllImport("opengl32.dll", SetLastError = true)]
    //public static extern void glClear(uint mask);

    //[DllImport("opengl32.dll", SetLastError = true)]
    //public static extern void glClearColor(float red, float green, float blue, float alpha);

    //public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
    //public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
    //public const uint GL_COLOR_BUFFER_BIT = 0x00004000;

    private static ConcurrentQueue<Action> actionQueue = new();

    private bool isRunning = true;

    public void Dispose()
    {
        window.Dispose();
    }

    public void Run()
    {
        var thread = new Thread(RenderThread);

        actionQueue.Enqueue(() => Console.WriteLine("Hello world from RenderThread!"));

        thread.Start();

        window.Initialize();

        renderer.Initialize();

        while (isRunning)
        {
            if(input.IsKeyPressed(KeyCodes.A))
            {
                actionQueue.Enqueue(() => Console.WriteLine("Hello A from RenderThread (from GameLoop)!"));
            }

            if (input.IsKeyPressed(KeyCodes.D))
            {
                actionQueue.Enqueue(() => Console.WriteLine("Hello D from RenderThread (from GameLoop)!"));
            }

            renderer.RenderTriangle();

            //glClear(GL_COLOR_BUFFER_BIT);
            //glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            window.Update();
            isRunning = window.IsRunning;
        }

        thread.Join();
    }

    private void RenderThread()
    {
        while(isRunning)
        {
            if (actionQueue.IsEmpty)
            {
                continue;
            }

            if (actionQueue.TryDequeue(out var action) is false)
            {
                continue;
            }

            action();
        }
    }
}
