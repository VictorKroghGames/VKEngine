namespace VKEngine.DependencyInjection;

public static class VKEngineContainerBuilderExtensions
{
    public static IVKEngineContainerBuilder AddGraphicsModule(this IVKEngineContainerBuilder containerBuilder)
    {
        containerBuilder.AddVulkanGraphics();

        return containerBuilder;
    }
}
