namespace VKEngine.Platform;

public sealed class WindowResizeEvent(int width, int height) : EventBase
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
