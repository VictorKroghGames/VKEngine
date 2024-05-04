using VKEngine.Configuration;

namespace VKEngine.Core.Configuration;

public class VKEngineConfiguration : IVKEngineConfiguration
{
    public required IPlatformConfiguration PlatformConfiguration { get; set; }
}
