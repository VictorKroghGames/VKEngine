using Microsoft.Extensions.DependencyInjection;

namespace VKEngine.DependencyInjection;

public interface IVKEngineContainerBuilder
{
    IServiceCollection Services { get; }

    IVKEngineContainer Build();
}
