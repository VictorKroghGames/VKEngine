using VKEngine.Configuration;
using VKEngine.Core;
using VKEngine.DependencyInjection;

var builder = Engine.CreateBuilder<SandboxApplication>(args);

builder.AddConfiguration<VKEngineConfiguration>(config =>
{
    config.PlatformConfiguration = new PlatformConfiguration
    {
        WindowTitle = "VKEngine Sandbox",
        WindowWidth = 1024,
        WindowHeight = 768
    };
    config.GraphicsConfiguration = new GraphicsConfiguration
    {
    };
});
builder.AddPlatformModule();
builder.AddGraphicsModule();

var app = builder.Build();

app.Run();
