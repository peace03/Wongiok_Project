using System;

public static class EventBus<T> where T : struct
{
    public static event Action<T> action;

    public static void Publish(T eventData) => action?.Invoke(eventData);
}
