using RetakesPluginShared.Enums;

namespace RetakesPluginShared.Events;

public record AnnounceBombsiteEvent(Bombsite Bombsite) : IRetakesPluginEvent;
