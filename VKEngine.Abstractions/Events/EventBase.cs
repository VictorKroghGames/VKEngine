namespace VKEngine;

public interface IEvent
{
    bool Handled { get; set; }
}

public abstract class EventBase : IEvent
{
    public bool Handled { get; set; }
}
