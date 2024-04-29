using VKEngine.Core;
using VKEngine.DependencyInjection;

var builder = Engine.CreateBuilder<SandboxApplication>(args);

builder.AddPlatformModule();
builder.AddGraphicsModule();

var app = builder.Build();

app.Run();
