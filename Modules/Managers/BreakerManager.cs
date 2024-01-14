using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class BreakerManager
{
    private readonly bool _isBreakerEnabled;
    private readonly bool _shouldOpenDoors;

    public BreakerManager(bool? isBreakerEnabled, bool? shouldOpenDoors)
    {
        _isBreakerEnabled = isBreakerEnabled ?? false;
        _shouldOpenDoors = shouldOpenDoors ?? false;
    }

    public void Handle()
    {
        if (!_isBreakerEnabled)
        {
            return;
        }

        DestroyBreakables();

        if (_shouldOpenDoors)
        {
            OpenDoors();
        }
    }

    private static void DestroyBreakables()
    {
        var breakableEntities = new List<(string designerName, string action, Type entityType)>
        {
            ("func_breakable", "Break", typeof(CBreakable)),
            ("func_breakable_surf", "Break", typeof(CBreakable)),
            ("prop.breakable.01", "Break", typeof(CBreakableProp)),
            ("prop.breakable.02", "Break", typeof(CBreakableProp)),
            ("prop_dynamic", "Break", typeof(CDynamicProp)),
            ("func_button", "Kill", typeof(CBaseButton))
        };

        var entitiesToBreak = new List<CEntityIdentity>();

        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity != null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            if (pEntity.DesignerName == null)
            {
                continue;
            }

            foreach (var (designerName, action, type) in breakableEntities)
            {
                if (
                    pEntity.DesignerName == designerName
                    && pEntity.GetType() == type
                )
                {
                    pEntity.AcceptInput(action);
                    break;
                }
            }
        }

        if (Server.MapName == "de_vertigo" || Server.MapName == "de_cache" || Server.MapName == "de_nuke")
        {
            breakableEntities.Add(("prop_dynamic", "Break", typeof(CDynamicProp)));
        }

        if (Server.MapName == "de_nuke")
        {
            breakableEntities.Add(("func_button", "Kill", typeof(CBaseButton)));
        }

        foreach (var (designerName, action, entityType) in breakableEntities)
        {
            IEnumerable<object> entities = entityType switch
            {
                Type et when et == typeof(CBreakable) => Utilities.FindAllEntitiesByDesignerName<CBreakable>(
                    designerName),
                Type et when et == typeof(CBreakableProp) => Utilities.FindAllEntitiesByDesignerName<CBreakableProp>(
                    designerName),
                Type et when et == typeof(CDynamicProp) => Utilities.FindAllEntitiesByDesignerName<CDynamicProp>(
                    designerName),
                Type et when et == typeof(CBaseButton) => Utilities.FindAllEntitiesByDesignerName<CBaseButton>(
                    designerName),
                _ => throw new InvalidOperationException("Unsupported entity type")
            };

            foreach (var entity in entities)
            {
                if (entity is CBreakable breakable)
                {
                    breakable.AcceptInput("Break");
                }
                else if (entity is CBreakableProp breakableProp)
                {
                    breakableProp.AcceptInput("Break");
                }
                else if (entity is CDynamicProp dynamicProp)
                {
                    dynamicProp.AcceptInput("Break");
                }
                else if (entity is CBaseButton baseButton)
                {
                    baseButton.AcceptInput("Break");
                }
            }
        }
    }

    private static void OpenDoors()
    {
        // TODO: It'll probably be more efficient to do it during the entity loop above.
        var doorEntities = Utilities.FindAllEntitiesByDesignerName<CPropDoorRotating>("prop_door_rotating");

        foreach (var doorEntity in doorEntities)
        {
            doorEntity.AcceptInput("open");
        }
    }
}