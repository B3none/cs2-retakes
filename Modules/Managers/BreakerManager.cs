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
        var entityActions = new List<(string designerName, string action)>();

        if (_shouldBreakBreakables)
        {
            entityActions.AddRange(new List<(string designerName, string action)>
            {
                ("func_breakable", "Break"),
                ("func_breakable_surf", "Break"),
                ("prop.breakable.01", "Break"),
                ("prop.breakable.02", "Break")
            });
            
            if (Server.MapName == "de_vertigo" || Server.MapName == "de_nuke")
            {
                entityActions.Add(("prop_dynamic", "Break"));
            }

            if (Server.MapName == "de_nuke")
            {
                entityActions.Add(("func_button", "Kill"));
            }
        }

        if (_shouldOpenDoors)
        {
            entityActions.Add(("prop_door_rotating", "open"));
        }

        if (entityActions.Count == 0)
        {
            return;
        }
        
        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity != null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            foreach (var (designerName, action) in entityActions)
            {
                if (pEntity.DesignerName == designerName)
                {
                    switch (pEntity.DesignerName)
                    {
                        case "func_breakable":
                        case "func_breakable_surf":
                            new CBreakable(pEntity.Handle).AcceptInput(action);
                            break;
                        
                        case "prop.breakable.01":
                        case "prop.breakable.02":
                            new CBreakableProp(pEntity.Handle).AcceptInput(action);
                            break;
                        
                        case "prop_dynamic":
                            new CDynamicProp(pEntity.Handle).AcceptInput(action);
                            break;
                        
                        case "func_button":
                            new CBaseButton(pEntity.Handle).AcceptInput(action);
                            break;
                        
                        case "prop_door_rotating":
                            new CPropDoorRotating(pEntity.Handle).AcceptInput(action);
                            break;
                    }
                    break;
                }
            }
        }
    }
}
