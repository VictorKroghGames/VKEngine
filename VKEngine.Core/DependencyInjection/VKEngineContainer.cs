using Microsoft.Extensions.DependencyInjection;

namespace VKEngine.DependencyInjection;

internal sealed class VKEngineContainer(IServiceProvider serviceProvider) : IVKEngineContainer
{
    public void Run()
    {
        using var scope = serviceProvider.CreateScope();

        using var application = scope.ServiceProvider.GetRequiredService<IApplication>();

        application.Run();
    }
}
