using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using VKEngine.Configuration;
using static ImGuiNET.ImGui;

namespace VKEngine.Graphics.ImGui;

public interface IImGuiRenderer
{
    void Initialize();

    void Begin();
    void End();
}

internal sealed class ImGuiRenderer(IVKEngineConfiguration engineConfiguration, IShaderFactory shaderFactory, IPipelineFactory pipelineFactory, IBufferFactory bufferFactory) : IImGuiRenderer
{
    private IPipeline pipeline;
    private IBuffer vertexBuffer;
    private IBuffer indexBuffer;

    private ulong vertexBufferSize;
    private uint indexBufferSize;

    private IntPtr contextPtr;

    private bool frameBegun = false;
    private Vector2 scaleFactor = Vector2.One;
    private static int sizeOfImDrawVert = Unsafe.SizeOf<ImDrawVert>();

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
        DestroyContext();
    }

    public void DrawDemoWindow()
    {
        ShowDemoWindow();
    }

    private void CreateDeviceResources()
    {
        vertexBufferSize = 10000;
        indexBufferSize = 2000;

        var shader = shaderFactory.CreateShader("ImGui",
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "shaders", "imgui.vert.spv"), ShaderModuleType.Vertex),
            new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "shaders", "imgui.frag.spv"), ShaderModuleType.Fragment)
        );

        pipeline = pipelineFactory.CreateGraphicsPipeline(new PipelineSpecification
        {
            PipelineLayout = new PipelineLayout(0, (uint)sizeOfImDrawVert, VertexInputRate.Vertex,
                new PipelineLayoutVertexAttribute(0, 1, Format.R32g32Sfloat, 0),            // position
                new PipelineLayoutVertexAttribute(0, 2, Format.R32g32Sfloat, 0),            // uv
                new PipelineLayoutVertexAttribute(0, 3, Format.R32g32b32a32Sfloat, 0)       // color
            ),
            Shader = shader
        });
        vertexBuffer = bufferFactory.CreateVertexBuffer(vertexBufferSize);
        indexBuffer = bufferFactory.CreateIndexBuffer<ushort>(indexBufferSize);
    }

    private void SetPerFrameImGuiData(float deltaTime)
    {
        var io = GetIO();
        io.DisplaySize = new Vector2(engineConfiguration.PlatformConfiguration.WindowWidth / scaleFactor.X, engineConfiguration.PlatformConfiguration.WindowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaTime;
    }
}
