namespace VKEngine;

public interface IEventDispatcher
{
    bool Dispatch<TEvent>(IEvent e, Func<TEvent, bool> func) where TEvent : IEvent;
}