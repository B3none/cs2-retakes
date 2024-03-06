using RetakesPluginShared.Events;

namespace RetakesPluginShared;

public interface IRetakesPluginEventSender
{
    event EventHandler<IRetakesPluginEvent> RetakesPluginEventHandlers;

    void AddEventListener(EventHandler<IRetakesPluginEvent> listener);
    
    void TriggerEvent(IRetakesPluginEvent @event);
}
