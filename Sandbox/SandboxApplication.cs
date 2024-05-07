using System.Collections.Concurrent;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Graphics.Enumerations;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IInput input, ITestRenderer testRenderer, IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary, ISwapChain swapChain, IPipelineFactory pipelineFactory, IRenderPassFactory renderPassFactory, ICommandPoolFactory commandPoolFactory, IVertexBufferFactory vertexBufferFactory) : IApplication
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

        shaderLibrary.Load("khronos_vulkan_vertex_buffer",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.frag.spv"), ShaderModuleType.Fragment)
        );

        var pipeline = pipelineFactory.CreateGraphicsPipeline(new PipelineSpecification
        {
            CullMode = CullMode.Back,
            FrontFace = FrontFace.Clockwise,
            Shader = shaderLibrary.Get("khronos_vulkan_vertex_buffer") ?? throw new InvalidOperationException("Shader not found!"),
            PipelineLayout = new PipelineLayout(0, (2 + 3) * sizeof(float), VertexInputRate.Vertex,
                                    new PipelineLayoutVertexAttribute(0, 0, Format.R32g32Sfloat, 0),  // POSITION
                                    new PipelineLayoutVertexAttribute(0, 1, Format.R32g32b32Sfloat, 2 * sizeof(float))   // COLOR
                            ),
            RenderPass = renderPass
        });

        var commandPool = commandPoolFactory.CreateCommandPool();

        var commandBuffer = commandPool.AllocateCommandBuffer();

        var vertexBuffer = vertexBufferFactory.CreateVertexBuffer();
        vertexBuffer.SetData();

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

            commandBuffer.BindBuffer(vertexBuffer);

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

        vertexBuffer.Cleanup();

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
