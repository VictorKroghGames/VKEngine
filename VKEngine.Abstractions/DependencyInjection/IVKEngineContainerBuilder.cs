using Microsoft.Extensions.DependencyInjection;
using VKEngine.Configuration;

namespace VKEngine.DependencyInjection;

public interface IVKEngineContainerBuilder
{
    IServiceCollection Services { get; }
    IVKEngineContainer Build();
}
