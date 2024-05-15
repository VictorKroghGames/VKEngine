namespace VKEngine.Platform;

public sealed class WindowFocusEvent(bool focused) : EventBase
{
    public bool Focused { get; } = focused;
}