using Microsoft.Extensions.DependencyInjection;
using VKEngine.Configuration;

namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddConfiguration<TEngineConfiguration>(this IVKEngineContainerBuilder containerBuilder, Action<TEngineConfiguration> configure)
        where TEngineConfiguration : class, IVKEngineConfiguration
    {
        containerBuilder.Services.AddSingleton<IVKEngineConfiguration, TEngineConfiguration>(x =>
        {
            var configuration = Activator.CreateInstance<TEngineConfiguration>();
            configure(configuration);
            return configuration;
        });

        return containerBuilder;
    }
}
