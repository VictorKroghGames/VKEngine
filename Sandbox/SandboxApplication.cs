using System.Collections.Concurrent;
using System.Numerics;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Graphics.Enumerations;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IInput input, /*ITestRenderer testRenderer, */IGraphicsContext graphicsContext, IShaderLibrary shaderLibrary, ISwapChain swapChain, IPipelineFactory pipelineFactory, IRenderPassFactory renderPassFactory, ICommandBufferAllocator commandBufferAllocator, IVertexBufferFactory vertexBufferFactory) : IApplication
{
    private static ConcurrentQueue<Action> actionQueue = new();

    private bool isRunning = true;

    internal readonly struct Vertex(Vector2 position, Vector3 color)
    {
        public readonly Vector2 Position = position;
        public readonly Vector3 Color = color;
    }

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

        commandBufferAllocator.Initialize();

        var commandBuffer = commandBufferAllocator.AllocateCommandBuffer();

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
                                    new PipelineLayoutVertexAttribute(0, 1, Format.R32g32b32Sfloat, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Vector2>())   // COLOR
                            ),
            RenderPass = renderPass
        });

        var vertexBuffer = vertexBufferFactory.CreateVertexBuffer();

        vertexBuffer.SetData([
            new Vertex(new Vector2(0.0f, -0.5f), new Vector3(1.0f, 0.0f, 0.0f)),
            new Vertex(new Vector2(0.5f, 0.5f), new Vector3(0.0f, 1.0f, 0.0f)),
            new Vertex(new Vector2(-0.5f, 0.5f), new Vector3(0.0f, 0.0f, 1.0f)),
        ]);

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

        commandBufferAllocator.Cleanup();

        vertexBuffer.Cleanup();

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
