using VKEngine.Configuration;

namespace VKEngine.DependencyInjection;

public class PlatformConfiguration : IPlatformConfiguration
{
    public required string WindowTitle { get; init; }
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public bool IsResizable { get; init; } = false;
}
