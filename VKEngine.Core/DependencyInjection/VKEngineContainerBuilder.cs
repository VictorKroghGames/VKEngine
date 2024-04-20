using Microsoft.Extensions.DependencyInjection;
using VKEngine.DependencyInjection;
using VKEngine.Platform;

namespace VKEngine.Core.DependencyInjection;

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
        if (serviceCollection.Any(x => x.ServiceType.Equals(typeof(IWindow))) is false)
        {
            serviceCollection.AddScoped<IWindow, NullWindow>();
        }

        serviceCollection.AddSingleton<IApplication, TApplication>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return new VKEngineContainer(serviceProvider);
    }
}

internal sealed class NullWindow : IWindow
{
    public bool IsRunning => true;

    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    public void Update()
    {
    }
}
