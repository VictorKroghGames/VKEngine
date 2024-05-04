using VKEngine.Configuration;

namespace VKEngine.Configuration;

public sealed class VKEngineConfiguration : IVKEngineConfiguration
{
    public required IPlatformConfiguration PlatformConfiguration { get; set; }
    public required IGraphicsConfiguration GraphicsConfiguration { get; set; }
}
