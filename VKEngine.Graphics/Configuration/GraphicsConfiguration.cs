namespace VKEngine.Configuration;

public sealed class GraphicsConfiguration : IGraphicsConfiguration
{
    public bool EnableVSync { get; init; } = true;
}
