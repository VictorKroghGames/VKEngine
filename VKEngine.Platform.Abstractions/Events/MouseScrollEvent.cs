namespace VKEngine.Platform;

public sealed class MouseScrollEvent(float xOffset, float yOffset) : EventBase
{
    public float XOffset { get; } = xOffset;
    public float YOffset { get; } = yOffset;
}