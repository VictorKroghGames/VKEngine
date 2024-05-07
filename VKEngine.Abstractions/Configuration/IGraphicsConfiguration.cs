namespace VKEngine.Configuration;

public interface IGraphicsConfiguration : IConfiguration
{
    uint FramesInFlight { get; init; }
    bool EnableVSync { get; init; }
    bool EnableImGui { get; init; }
}