namespace VKEngine.Platform;

public abstract class KeyEventBase(int keyCode) : EventBase
{
    public int KeyCode { get; } = keyCode;
}
