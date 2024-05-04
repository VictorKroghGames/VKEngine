namespace VKEngine.Configuration;

public interface IConfiguration
{
}

public interface IVKEngineConfiguration : IConfiguration
{
    IPlatformConfiguration PlatformConfiguration { get; set; }
    IGraphicsConfiguration GraphicsConfiguration { get; set; }
}
