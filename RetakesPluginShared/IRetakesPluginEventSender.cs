using RetakesPluginShared.Events;

namespace RetakesPluginShared;

public interface IRetakesPluginEventSender
{
    public event EventHandler<IRetakesPluginEvent> RetakesPluginEventHandlers;
    public void TriggerEvent(IRetakesPluginEvent @event);
}
