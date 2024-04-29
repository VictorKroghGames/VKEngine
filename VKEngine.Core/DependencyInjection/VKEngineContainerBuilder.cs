using Microsoft.Extensions.DependencyInjection;

namespace VKEngine.DependencyInjection;

internal sealed class VKEngineContainerBuilder<TApplication> : IVKEngineContainerBuilder
    where TApplication : class, IApplication
{
    private readonly ServiceCollection serviceCollection;

    public VKEngineContainerBuilder()
    {
        serviceCollection = new ServiceCollection();
    }

    public IServiceCollection Services => serviceCollection;

    public IVKEngineContainer Build()
    {
        serviceCollection.AddSingleton<IApplication, TApplication>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return new VKEngineContainer(serviceProvider);
    }
}
