namespace VKEngine.Configuration;

public interface IPlatformConfiguration : IConfiguration
{
    string WindowTitle { get; init; }
    int WindowWidth { get; init; }
    int WindowHeight { get; init; }
    bool IsResizable { get; init; }
}
