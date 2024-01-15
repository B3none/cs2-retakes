using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class BreakerManager
{
    private readonly List<(string designerName, string action)> _entityActions = new();

    private static readonly HashSet<string> MapsWithPropDynamic = new()
    {
        "de_vertigo",
        "de_nuke",
        "de_mirage"
    };

    private static readonly HashSet<string> MapsWithFuncButton = new()
    {
        "de_nuke"
    };

    public BreakerManager(bool? shouldBreakBreakables, bool? shouldOpenDoors)
    {
        if (shouldBreakBreakables ?? false)
        {
            _entityActions.AddRange(new List<(string designerName, string action)>
            {
                ("func_breakable", "Break"),
                ("func_breakable_surf", "Break"),
                ("prop.breakable.01", "Break"),
                ("prop.breakable.02", "Break")
            });
            
            if (MapsWithPropDynamic.Contains(Server.MapName))
            {
                _entityActions.Add(("prop_dynamic", "Break"));
            }

            if (MapsWithFuncButton.Contains(Server.MapName))
            {
                _entityActions.Add(("func_button", "Kill"));
            }
        }

        if (shouldOpenDoors ?? false)
        {
            _entityActions.Add(("prop_door_rotating", "open"));
        }
    }

    public void Handle()
    {
        if (_entityActions.Count == 0)
        {
            return;
        }
        
        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity != null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            foreach (var (designerName, action) in _entityActions)
            {
                if (pEntity.DesignerName != designerName)
                {
                    continue;
                }
                
                switch (pEntity.DesignerName)
                {
                    case "func_breakable":
                    case "func_breakable_surf":
                        new PointerTo<CBreakable>(pEntity.Handle).Value.AcceptInput(action);
                        break;
                        
                    case "prop.breakable.01":
                    case "prop.breakable.02":
                        new PointerTo<CBreakableProp>(pEntity.Handle).Value.AcceptInput(action);
                        break;
                        
                    case "prop_dynamic":
                        new PointerTo<CDynamicProp>(pEntity.Handle).Value.AcceptInput(action);
                        break;
                        
                    case "func_button":
                        new PointerTo<CBaseButton>(pEntity.Handle).Value.AcceptInput(action);
                        break;
                        
                    case "prop_door_rotating":
                        new PointerTo<CPropDoorRotating>(pEntity.Handle).Value.AcceptInput(action);
                        break;
                }
                break;
            }
        }
    }
}
