using VKEngine.Core;
using VKEngine.DependencyInjection;

var builder = Engine.CreateBuilder<SandboxApplication>(args);

builder.AddPlatformModule();

var app = builder.Build();

app.Run();
