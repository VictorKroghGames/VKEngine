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
}

internal sealed class ImGuiRenderer(IVKEngineConfiguration engineConfiguration, IShaderFactory shaderFactory, IPipelineFactory pipelineFactory, IBufferFactory bufferFactory, ITextureFactory textureFactory) : IImGuiRenderer
{
    private struct ProjectionMatrixBuffer
    {
        Matrix4x4 projection_matrix;
    };

    private IBuffer vertexBuffer = default!;
    private IBuffer indexBuffer = default!;
    private ITexture fontTexture = default!;
    private IBuffer uniformBuffer = default!;
    private IShader shader = default!;

    private ulong vertexBufferSize;
    private uint indexBufferSize;

    private IntPtr contextPtr;

    private bool frameBegun = false;
    private Vector2 scaleFactor = Vector2.One;
    private static int sizeOfImDrawVert = Unsafe.SizeOf<ImDrawVert>();

    public ITexture FontTexture => fontTexture;

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
        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            var commandList = draw_data.CmdLists[i];

        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);
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

        //var vertexLayout = new VertexLayoutDescription(binding: 0u,
        //    new VertexLayoutElementDescription("in_position",   Format.R32g32Sfloat),
        //    new VertexLayoutElementDescription("in_texCoord",   Format.R32g32Sfloat),
        //    new VertexLayoutElementDescription("in_color",      Format.R8g8b8a8Snorm)
        //);

        //var pipelineSpecification = new PipelineDescription
        //{
        //    VertexLayout = vertexLayout,
        //    Shader = shader
        //};
    }

    private void RecreateFontDeviceTexture()
    {
        var io = GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        fontTexture = textureFactory.CreateTextureFromMemory(width, height, pixels, (uint)(width * height * bytesPerPixel));

        io.Fonts.SetTexID(1);
    }

    private void SetPerFrameImGuiData(float deltaTime)
    {
        var io = GetIO();
        io.DisplaySize = new Vector2(engineConfiguration.PlatformConfiguration.WindowWidth / scaleFactor.X, engineConfiguration.PlatformConfiguration.WindowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaTime;
    }
}
