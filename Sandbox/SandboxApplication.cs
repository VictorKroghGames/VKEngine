using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using VKEngine;
using VKEngine.Graphics;
using VKEngine.Graphics.ImGui;
using VKEngine.Platform;

internal sealed class SandboxApplication(IWindow window, IEventDispatcher eventDispatcher, IInput input, IShaderLibrary shaderLibrary, IRenderer renderer, ISwapChain swapChain, IPipelineFactory pipelineFactory, IRenderPassFactory renderPassFactory, IBufferFactory bufferFactory, IDescriptorSetFactory descriptorSetFactory, ITextureFactory textureFactory, IImGuiRenderer imGuiRenderer) : IApplication
{
    private static ConcurrentQueue<Action> actionQueue = new();

    private bool isRunning = true;

    internal readonly struct Vertex(Vector2 position, Vector3 color, Vector2 texCoord)
    {
        public readonly Vector2 Position = position;
        public readonly Vector3 Color = color;
        public readonly Vector2 TexCoord = texCoord;
    }

    internal struct UniformBufferObject
    {
        public Matrix4x4 Model { get; set; }
        public Matrix4x4 View { get; set; }
        public Matrix4x4 Projection { get; set; }
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
        window.SetEventCallback(OnEvent);

        renderer.Initialize();

        swapChain.Initialize();

        imGuiRenderer.Initialize();

        shaderLibrary.Load("khronos_vulkan_vertex_buffer",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.frag.spv"), ShaderModuleType.Fragment)
        );

        shaderLibrary.Load("khronos_vulkan_uniform_buffer",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_uniform_buffer.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_uniform_buffer.frag.spv"), ShaderModuleType.Fragment)
        );

        shaderLibrary.Load("khronos_vulkan_texture",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_texture.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_texture.frag.spv"), ShaderModuleType.Fragment)
        );

        var uniformBufferData = new UniformBufferObject
        {
            Model = Matrix4x4.Identity,
            View = Matrix4x4.CreateTranslation(0.0f, 0.0f, -2.0f),
            Projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, window.Width / (float)window.Height, 0.1f, 10.0f)
        };

        var projection = uniformBufferData.Projection;
        projection.M11 *= -1;
        uniformBufferData.Projection = projection;

        var uniformBuffer = bufferFactory.CreateUniformBuffer<UniformBufferObject>();
        uniformBuffer.UploadData(ref uniformBufferData);

        var texture = textureFactory.CreateTextureFromFilePath(Path.Combine(AppContext.BaseDirectory, "Textures", "texture.jpg"));

        var descriptorSet = descriptorSetFactory.CreateDescriptorSet(new DescriptorSetDescription
        {
            DescriptorBindings = [
                new DescriptorBinding {
                    Binding = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    ShaderStageFlags = ShaderModuleType.Vertex
                },
                new DescriptorBinding {
                    Binding = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    ShaderStageFlags = ShaderModuleType.Fragment
                }
            ]
        });

        var vertexLayout = new VertexLayout(
            new VertexLayoutAttribute("inPosition", Format.R32g32Sfloat),
            new VertexLayoutAttribute("inColor", Format.R32g32b32Sfloat),
            new VertexLayoutAttribute("inTexCoord", Format.R32g32Sfloat)
        );

        var pipeline = pipelineFactory.CreateGraphicsPipeline(new PipelineDescription
        {
            Shader = shaderLibrary.Get("khronos_vulkan_texture") ?? throw new InvalidOperationException("Shader not found!"),
            VertexLayouts = [vertexLayout],
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            CullMode = CullMode.Back,
            FrontFace = FrontFace.CounterClockwise,
            DescriptorSets = [descriptorSet]
        });

        var vertexBuffer = bufferFactory.CreateVertexBuffer((ulong)(4 * Unsafe.SizeOf<Vertex>()));

        vertexBuffer.UploadData([
            new Vertex(new Vector2(-0.5f, -0.5f), new Vector3(1.0f, 0.0f, 0.0f), new Vector2(1.0f, 0.0f)),
            new Vertex(new Vector2( 0.5f, -0.5f), new Vector3(0.0f, 1.0f, 0.0f), new Vector2(0.0f, 0.0f)),
            new Vertex(new Vector2( 0.5f,  0.5f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 1.0f)),
            new Vertex(new Vector2(-0.5f,  0.5f), new Vector3(1.0f, 1.0f, 1.0f), new Vector2(1.0f, 1.0f)),
        ]);

        var indexBuffer = bufferFactory.CreateIndexBuffer<ushort>(6);

        indexBuffer.UploadData(new ushort[] { 0, 1, 2, 2, 3, 0 });

        //{
        //    //uniformBufferData.Model = Matrix4x4.CreateRotationZ((float)DateTime.Now.TimeOfDay.TotalSeconds);
        //    uniformBufferData.View = Matrix4x4.CreateTranslation(0.0f, 0.0f, -2.0f);
        //    uniformBufferData.Projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, window.Width / (float)window.Height, 0.1f, 10.0f);

        //    var projection = uniformBufferData.Projection;
        //    projection.M11 *= -1;
        //    uniformBufferData.Projection = projection;

        //    uniformBuffer.UploadData(ref uniformBufferData);
        //}

        descriptorSet.Update<UniformBufferObject>(0, 0, uniformBuffer);
        descriptorSet.Update(1, 0, texture);

        while (isRunning)
        {
            //if (input.IsKeyPressed(KeyCodes.A))
            //{
            //    actionQueue.Enqueue(() => Console.WriteLine("Hello A from RenderThread (from GameLoop)!"));
            //}

            //if (input.IsKeyPressed(KeyCodes.D))
            //{
            //    actionQueue.Enqueue(() => Console.WriteLine("Hello D from RenderThread (from GameLoop)!"));
            //}

            // Update uniform buffer
            //{
            //    uniformBufferData.Model = Matrix4x4.CreateRotationZ((float)DateTime.Now.TimeOfDay.TotalSeconds);
            //    uniformBufferData.View = Matrix4x4.CreateTranslation(0.0f, 0.0f, -2.0f);
            //    uniformBufferData.Projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, window.Width / (float)window.Height, 0.1f, 10.0f);

            //    var projection = uniformBufferData.Projection;
            //    projection.M11 *= -1;
            //    uniformBufferData.Projection = projection;

            //    uniformBuffer.UploadData(ref uniformBufferData);
            //}

            //renderer.BeginFrame();
            //renderer.Draw(swapChain.RenderPass, pipeline, vertexBuffer, indexBuffer, descriptorSet);
            //renderer.EndFrame();

            swapChain.AquireNextImage();

            imGuiRenderer.Begin();
            imGuiRenderer.DrawDemoWindow();
            imGuiRenderer.End();

            //renderer.Render();
            swapChain.Present();

            window.Update();
            isRunning = window.IsRunning;
        }

        renderer.Wait();

        texture.Cleanup();
        imGuiRenderer.Cleanup();

        uniformBuffer.Cleanup();
        indexBuffer.Cleanup();
        vertexBuffer.Cleanup();

        descriptorSet.Cleanup();

        pipeline.Cleanup();

        swapChain.Cleanup();

        shaderLibrary.Cleanup();

        renderer.Cleanup();

        thread.Join();
    }

    private void OnEvent(IEvent e)
    {
        Console.WriteLine($"Event: {e.GetType().Name}");
        eventDispatcher.Dispatch<WindowCloseEvent>(e, OnWindowCloseEventFunc);
        eventDispatcher.Dispatch<KeyPressedEvent>(e, OnKeyPressedEventFunc);
        eventDispatcher.Dispatch<KeyReleasedEvent>(e, OnKeyReleasedEventFunc);
    }

    private bool OnWindowCloseEventFunc(WindowCloseEvent windowCloseEvent)
    {
        isRunning = false;
        return true;
    }

    private bool OnKeyPressedEventFunc(KeyPressedEvent keyPressedEvent)
    {
        Console.WriteLine($"Key Pressed Event: {keyPressedEvent.KeyCode}");
        return true;
    }

    private bool OnKeyReleasedEventFunc(KeyReleasedEvent keyReleasedEvent)
    {
        Console.WriteLine($"Key Released Event: {keyReleasedEvent.KeyCode}");
        return true;
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
