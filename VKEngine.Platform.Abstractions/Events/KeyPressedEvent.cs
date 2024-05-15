namespace VKEngine.Platform;

public sealed class KeyPressedEvent(int keyCode, bool repeat) : KeyEventBase(keyCode)
{
    public bool Repeat { get; } = repeat;
}
