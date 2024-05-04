using Microsoft.Extensions.DependencyInjection;
using VKEngine.Configuration;

namespace VKEngine.DependencyInjection;

public static  class IVKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddConfiguration<TConfiguration>(this IVKEngineContainerBuilder containerBuilder, Func<IVKEngineConfiguration, TConfiguration> configurationGetter)
        where TConfiguration : class, IConfiguration
    {
        containerBuilder.Services.AddSingleton<TConfiguration>(serviceProvider => configurationGetter(serviceProvider.GetRequiredService<IVKEngineConfiguration>()));

        return containerBuilder;
    }
}
