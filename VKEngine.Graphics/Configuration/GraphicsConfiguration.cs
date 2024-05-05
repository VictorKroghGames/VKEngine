namespace VKEngine.Configuration;

public sealed class GraphicsConfiguration : IGraphicsConfiguration
{
    public uint FramesInFlight { get; init; } = 2;
    public bool EnableVSync { get; init; } = true;
}
