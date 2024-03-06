using RetakesPluginShared;
using RetakesPluginShared.Events;

namespace RetakesPlugin.Modules;

public class RetakesPluginEventSender: IRetakesPluginEventSender
{
    public event EventHandler<IRetakesPluginEvent>? RetakesPluginEventHandlers;

    public void AddEventListener(EventHandler<IRetakesPluginEvent> listener)
    {
        RetakesPluginEventHandlers += listener;
    }

    public void TriggerEvent(IRetakesPluginEvent @event)
    {
        RetakesPluginEventHandlers?.Invoke(this, @event);
    }
}
