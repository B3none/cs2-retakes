using RetakesPluginShared;
using RetakesPluginShared.Events;

namespace RetakesPlugin.Modules;

public class RetakesPluginEventSender : IRetakesPluginEventSender
{
    public event EventHandler<IRetakesPluginEvent>? RetakesPluginEventHandlers;

    public void TriggerEvent(IRetakesPluginEvent @event)
    {
        RetakesPluginEventHandlers?.Invoke(this, @event);
    }
}
