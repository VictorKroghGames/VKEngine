using Microsoft.Extensions.DependencyInjection;
using VKEngine.DependencyInjection;

namespace VKEngine.Core.DependencyInjection;

internal sealed class VKEngineContainer(IServiceProvider serviceProvider) : IVKEngineContainer
{
    public void Run()
    {
        using var scope = serviceProvider.CreateScope();

        using var application = scope.ServiceProvider.GetRequiredService<IApplication>();

        application.Run();
    }
}
