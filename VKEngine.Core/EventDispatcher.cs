namespace VKEngine.Core;

internal sealed class EventDispatcher : IEventDispatcher
{
    public bool Dispatch<TEvent>(IEvent e, Func<TEvent, bool> func) where TEvent : IEvent
    {
        if (e is null)
        {
            return false;
        }

        if (e is TEvent @event)
        {
            e.Handled |= func(@event);
            return e.Handled;
        }

        return false;
    }
}