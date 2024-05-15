namespace VKEngine.Platform;

public sealed class MousePositionEvent(double x, double y) : EventBase
{
    public double X { get; } = x;
    public double Y { get; } = y;
}
