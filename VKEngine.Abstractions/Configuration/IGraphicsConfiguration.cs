namespace VKEngine.Configuration;

public interface IGraphicsConfiguration : IConfiguration
{
    bool EnableVSync { get; init; }
}