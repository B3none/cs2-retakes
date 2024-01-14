using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace RetakesPlugin.Modules.Managers;

public class BreakerManager
{
    private readonly bool _shouldBreakBreakables;
    private readonly bool _shouldOpenDoors;

    public BreakerManager(bool? shouldBreakBreakables, bool? shouldOpenDoors)
    {
        _shouldBreakBreakables = shouldBreakBreakables ?? false;
        _shouldOpenDoors = shouldOpenDoors ?? false;
    }

    public void Handle()
    {
        var entityActions = new List<(string designerName, string action, Type entityType)>();

        if (_shouldBreakBreakables)
        {
            entityActions.AddRange(new List<(string designerName, string action, Type entityType)>
            {
                ("func_breakable", "Break", typeof(CBreakable)),
                ("func_breakable_surf", "Break", typeof(CBreakable)),
                ("prop.breakable.01", "Break", typeof(CBreakableProp)),
                ("prop.breakable.02", "Break", typeof(CBreakableProp))
            });
            
            if (Server.MapName == "de_vertigo" || Server.MapName == "de_nuke")
            {
                entityActions.Add(("prop_dynamic", "Break", typeof(CDynamicProp)));
            }

            if (Server.MapName == "de_nuke")
            {
                entityActions.Add(("func_button", "Kill", typeof(CBaseButton)));
            }
        }

        if (_shouldOpenDoors)
        {
            entityActions.Add(("prop_door_rotating", "open", typeof(CPropDoorRotating)));
        }

        if (entityActions.Count == 0)
        {
            return;
        }
        
        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity != null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            foreach (var (designerName, action, type) in entityActions)
            {
                if (pEntity.DesignerName == designerName)
                {
                    dynamic? entity = Activator.CreateInstance(type, pEntity.Handle);
                    
                    if (entity != null)
                    {
                        entity.AcceptInput(action);
                    }
                    
                    break;
                }
            }
        }
    }
}
