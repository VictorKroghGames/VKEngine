using VKEngine.DependencyInjection;

namespace VKEngine.Core;

public class Engine
{
    public static IVKEngineContainerBuilder CreateBuilder<TApplication>(string[] args)
        where TApplication : class, IApplication
    {
        return new VKEngineContainerBuilder<TApplication>();
    }
}
