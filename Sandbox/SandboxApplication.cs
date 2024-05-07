using System.Collections.Concurrent;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Graphics.Enumerations;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IInput input, ITestRenderer testRenderer, IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary, ISwapChain swapChain, IPipelineFactory pipelineFactory, IRenderPassFactory renderPassFactory, ICommandPoolFactory commandPoolFactory) : IApplication
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

        var renderPass = renderPassFactory.CreateRenderPass();

        swapChain.Initialize(renderPass);

        //shaderLibrary.Load("shader",
        //    new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv"), ShaderModuleType.Vertex),
        //    new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv"), ShaderModuleType.Fragment)
        //);

        shaderLibrary.Load("khronos_vulkan_tutorial",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_tutorial.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_tutorial.frag.spv"), ShaderModuleType.Fragment)
        );

        var pipeline = pipelineFactory.CreateGraphicsPipeline(new PipelineSpecification
        {
            CullMode = CullMode.Back,
            FrontFace = FrontFace.Clockwise,
            Shader = shaderLibrary.Get("khronos_vulkan_tutorial") ?? throw new InvalidOperationException("Shader not found!"),
            RenderPass = renderPass
        });

        var commandPool = commandPoolFactory.CreateCommandPool();

        var commandBuffer = commandPool.AllocateCommandBuffer();

        //testRenderer.Initialize();

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

            swapChain.AquireNextImage();

            commandBuffer.Begin();

            commandBuffer.BeginRenderPass(renderPass);

            commandBuffer.BindPipeline(pipeline);

            commandBuffer.Draw();

            commandBuffer.EndRenderPass();

            commandBuffer.End();

            commandBuffer.Submit();

            swapChain.Present();

            //testRenderer.RenderTriangle();

            window.Update();
            isRunning = window.IsRunning;
        }

        //testRenderer.Cleanup();

        commandPool.FreeCommandBuffer(commandBuffer);

        commandPool.Cleanup();

        pipeline.Cleanup();

        renderPass.Cleanup();

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
