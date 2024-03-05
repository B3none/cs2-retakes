namespace RetakesEvents.Contracts;

public interface IEventSender
{
    event EventHandler<PluginEventArgs> PluginEventOccurred;

    void CheckEventListeners();
    void AddEventListener(EventHandler<PluginEventArgs> listener);
    void TriggerEvent(string eventName, object eventData);
}

public class PluginEventArgs : EventArgs
{
    public string EventName { get; }
    public object EventData { get; }

    public PluginEventArgs(string eventName, object eventData)
    {
        EventName = eventName;
        EventData = eventData;
    }
}
