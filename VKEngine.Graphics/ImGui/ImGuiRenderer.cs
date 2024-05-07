using ImGuiNET;
using System.Numerics;
using VKEngine.Configuration;
using static ImGuiNET.ImGui;

namespace VKEngine.Graphics.ImGui;

public interface IImGuiRenderer
{
    void Initialize();

    void Begin();
    void End();
}

internal sealed class ImGuiRenderer(IVKEngineConfiguration engineConfiguration) : IImGuiRenderer
{
    private IntPtr contextPtr;

    private bool frameBegun = false;
    private Vector2 scaleFactor = Vector2.One;

    public void Initialize()
    {
        contextPtr = CreateContext();
        SetCurrentContext(contextPtr);

        var io = GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

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
        // RenderDrawData();

        var io = GetIO();
        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            var backupCurrentContext = GetCurrentContext();
            UpdatePlatformWindows();
            RenderPlatformWindowsDefault();
            SetCurrentContext(backupCurrentContext);
        }
    }

    public void Shutdown()
    {
        DestroyContext();
    }

    public void DrawDemoWindow()
    {
        ShowDemoWindow();
    }

    private void SetPerFrameImGuiData(float deltaTime)
    {
        var io = GetIO();
        io.DisplaySize = new Vector2(engineConfiguration.PlatformConfiguration.WindowWidth / scaleFactor.X, engineConfiguration.PlatformConfiguration.WindowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaTime;
    }
}
