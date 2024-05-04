namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddPlatformModule(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.AddConfiguration(x => x.PlatformConfiguration);

        containerBuilder.Services.AddGlfwPlatformModule();

        return containerBuilder;
    }
}
