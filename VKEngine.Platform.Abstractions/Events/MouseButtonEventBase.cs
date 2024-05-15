namespace VKEngine.Platform;

public abstract class MouseButtonEventBase(int mouseButton) : EventBase
{
    public int MouseButton { get; } = mouseButton;
}
