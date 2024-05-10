namespace VKEngine.Graphics.Enumerations;

public enum CommandBufferUsageFlags
{
    None = 0,
    OneTimeSubmit = 1,
    RenderPassContinue = 2,
    SimultaneousUse = 4
}
