using System.Collections.Concurrent;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IInput input, ITestRenderer testRenderer, IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary, ISwapChain swapChain) : IApplication
{
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

        graphicsContext.Initialize();

        shaderLibrary.Load("shader",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv"), ShaderModuleType.Fragment)
        );

        swapChain.Initialize();

        testRenderer.Initialize();

        while (isRunning)
        {
            if (input.IsKeyPressed(KeyCodes.A))
            {
                actionQueue.Enqueue(() => Console.WriteLine("Hello A from RenderThread (from GameLoop)!"));
            }

            if (input.IsKeyPressed(KeyCodes.D))
            {
                actionQueue.Enqueue(() => Console.WriteLine("Hello D from RenderThread (from GameLoop)!"));
            }

            testRenderer.RenderTriangle();

            window.Update();
            isRunning = window.IsRunning;
        }

        testRenderer.Cleanup();

        swapChain.Cleanup();

        graphicsContext.Cleanup();

        thread.Join();
    }

    private void RenderThread()
    {
        while (isRunning)
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
