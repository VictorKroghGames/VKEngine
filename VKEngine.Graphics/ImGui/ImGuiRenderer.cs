using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using static ImGuiNET.ImGui;

namespace VKEngine.Graphics.ImGui;

public interface IImGuiRenderer
{
    ITexture FontTexture { get; }

    void Initialize();
    void Cleanup();

    void Begin();
    void End();

    void DrawDemoWindow();
}

internal sealed class ImGuiRenderer(IVKEngineConfiguration engineConfiguration, IGraphicsContext graphicsContext, ISwapChain swapChain, IShaderFactory shaderFactory, IDescriptorSetFactory descriptorSetFactory, IPipelineFactory pipelineFactory, IBufferFactory bufferFactory, ITextureFactory textureFactory, IRenderPassFactory renderPassFactory, ICommandBufferAllocator commandBufferAllocator) : IImGuiRenderer
{
    private struct ProjectionMatrixBuffer
    {
        internal Matrix4x4 projection_matrix;
    };

    private IRenderPass renderPass = default!;
    private ICommandBuffer[] commandBuffers = [];

    private IPipeline pipeline = default!;
    private IBuffer vertexBuffer = default!;
    private IBuffer indexBuffer = default!;
    private ITexture fontTexture = default!;
    private IBuffer uniformBuffer = default!;
    private IShader shader = default!;

    private IDescriptorSet descriptorSet = default!;
    private IDescriptorSet descriptorSet1 = default!;

    private ulong vertexBufferSize;
    private uint indexBufferSize;

    private IntPtr contextPtr;

    private bool frameBegun = false;
    private Vector2 scaleFactor = Vector2.One;
    private static int sizeOfImDrawVert = Unsafe.SizeOf<ImDrawVert>();

    public ITexture FontTexture => fontTexture;
    private IntPtr fontAtlasId = 1;

    public void Initialize()
    {
        contextPtr = CreateContext();
        SetCurrentContext(contextPtr);

        var io = GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

        CreateDeviceResources();

        SetPerFrameImGuiData(1.0f / 60.0f);
    }

    public void Cleanup()
    {
        DestroyContext();

        vertexBuffer.Cleanup();
        indexBuffer.Cleanup();
        fontTexture.Cleanup();
        uniformBuffer.Cleanup();
        shader.Cleanup();
        descriptorSet.Cleanup();
        pipeline.Cleanup();
        descriptorSet1.Cleanup();
        renderPass.Cleanup();

        foreach (var commandBuffer in commandBuffers)
        {
            commandBuffer.Cleanup();
        }
    }

    public void Begin()
    {
        frameBegun = true;
        NewFrame();
    }

    public void End()
    {
        if (frameBegun is false)
        {
            return;
        }

        EndFrame();
        frameBegun = false;

        Render();
        RenderImDrawData(GetDrawData());

        var io = GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            var backupCurrentContext = GetCurrentContext();
            UpdatePlatformWindows();
            RenderPlatformWindowsDefault();
            SetCurrentContext(backupCurrentContext);
        }
    }

    // https://github.com/ImGuiNET/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram/ImGuiController.cs
    private void RenderImDrawData(ImDrawDataPtr draw_data)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        //if (draw_data.CmdListsCount == 0)
        //{
        //    return;
        //}

        var totalVBSize = (ulong)(draw_data.TotalVtxCount * sizeOfImDrawVert);
        if (totalVBSize > vertexBufferSize)
        {
            vertexBuffer.Cleanup();
            vertexBuffer = bufferFactory.CreateVertexBuffer(totalVBSize);
            vertexBufferSize = totalVBSize;
        }

        var totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > indexBufferSize)
        {
            indexBuffer.Cleanup();
            indexBuffer = bufferFactory.CreateIndexBuffer(totalIBSize);
            indexBufferSize = totalIBSize;
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            var commandList = draw_data.CmdLists[i];

            vertexBuffer.UploadData((uint)(vertexOffsetInVertices * sizeOfImDrawVert), (uint)(commandList.VtxBuffer.Size * sizeOfImDrawVert), commandList.VtxBuffer.Data);

            indexBuffer.UploadData((uint)(indexOffsetInElements * sizeof(ushort)), (uint)(commandList.IdxBuffer.Size * sizeof(ushort)), commandList.IdxBuffer.Data);

            vertexOffsetInVertices += (uint)commandList.VtxBuffer.Size;
            indexOffsetInElements += (uint)commandList.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = GetIO();
        var projectionMatrix = new ProjectionMatrixBuffer
        {
            projection_matrix = Matrix4x4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f)
        };

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        uniformBuffer.UploadData(ref projectionMatrix);

        var commandBuffer = commandBuffers[swapChain.CurrentFrameIndex];

        commandBuffer.Begin();

        commandBuffer.BeginRenderPass(renderPass);

        commandBuffer.BindPipeline(pipeline);

        commandBuffer.BindVertexBuffer(vertexBuffer);

        commandBuffer.BindIndexBuffer(indexBuffer);

        commandBuffer.BindDescriptorSet(pipeline, descriptorSet, 0);
        commandBuffer.BindDescriptorSet(pipeline, descriptorSet1, 1);

        int vtx_offset = 0;
        int idx_offset = 0;
        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            var commandList = draw_data.CmdLists[i];
            for (int j = 0; j < commandList.CmdBuffer.Size; j++)
            {
                var cmd = commandList.CmdBuffer[j];
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    throw new Exception("User callback is not supported!");
                }

                //commandBuffer.DrawIndex(cmd.ElemCount);
                commandBuffer.DrawIndexed(cmd.ElemCount, cmd.IdxOffset + (uint)idx_offset, (int)cmd.VtxOffset + vtx_offset);
            }

            vtx_offset += commandList.VtxBuffer.Size;
            idx_offset += commandList.IdxBuffer.Size;
        }

        commandBuffer.EndRenderPass();

        commandBuffer.End();

        commandBuffer.Submit();
    }

    public void Shutdown()
    {
    }

    public void DrawDemoWindow()
    {
        ShowDemoWindow();
    }

    private void CreateDeviceResources()
    {
        commandBuffers = new ICommandBuffer[engineConfiguration.GraphicsConfiguration.FramesInFlight];
        for (int i = 0; i < engineConfiguration.GraphicsConfiguration.FramesInFlight; i++)
        {
            commandBuffers[i] = commandBufferAllocator.AllocateCommandBuffer();
        }

        renderPass = renderPassFactory.CreateRenderPass(Format.B8g8r8a8Unorm);

        vertexBufferSize = 10000;
        indexBufferSize = 2000;

        vertexBuffer = bufferFactory.CreateVertexBuffer(vertexBufferSize);
        indexBuffer = bufferFactory.CreateIndexBuffer(indexBufferSize);

        RecreateFontDeviceTexture();

        uniformBuffer = bufferFactory.CreateUniformBuffer<ProjectionMatrixBuffer>();

        shader = shaderFactory.CreateShader("khronos_vulkan_vertex_buffer",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "khronos_vulkan_vertex_buffer.frag.spv"), ShaderModuleType.Fragment)
        );

        var vertexLayout = new VertexLayout(
            new VertexLayoutAttribute("in_position", Format.R32g32Sfloat),
            new VertexLayoutAttribute("in_texCoord", Format.R32g32Sfloat),
            new VertexLayoutAttribute("in_color", Format.R32g32b32a32Sfloat)
        );

        descriptorSet = descriptorSetFactory.CreateDescriptorSet(new DescriptorSetDescription
        {
            DescriptorBindings = [
                new DescriptorBinding
                {
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

        descriptorSet1 = descriptorSetFactory.CreateDescriptorSet(new DescriptorSetDescription
        {
            DescriptorBindings = [
                new DescriptorBinding {
                    Binding = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    ShaderStageFlags = ShaderModuleType.Fragment
                }
            ]
        });

        pipeline = pipelineFactory.CreateGraphicsPipeline(new PipelineDescription
        {
            Shader = shader,
            VertexLayouts = [vertexLayout],
            RenderPass = renderPass,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            CullMode = CullMode.None,
            FrontFace = FrontFace.Clockwise,
            DescriptorSets = [descriptorSet, descriptorSet1]
        });
    }

    private void RecreateFontDeviceTexture()
    {
        var io = GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        io.Fonts.SetTexID(fontAtlasId);

        fontTexture = textureFactory.CreateTextureFromMemory(width, height, pixels, (uint)(width * height * bytesPerPixel));

        io.Fonts.ClearTexData();
    }

    private void SetPerFrameImGuiData(float deltaTime)
    {
        var io = GetIO();
        io.DisplaySize = new Vector2(engineConfiguration.PlatformConfiguration.WindowWidth / scaleFactor.X, engineConfiguration.PlatformConfiguration.WindowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaTime;
    }
}
