using System.Collections;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunSelectionRadarChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.SetSettingsVisible(false);controller.SetMenuVisible(false);
            var p=controller.Simulation.FindPlanet(1);var tank=p.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Tank);var engineer=p.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Engineer);var third=p.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Artillery);
            PlaceDirectOrderUnit(tank,12,15);PlaceDirectOrderUnit(engineer,14,15);PlaceDirectOrderUnit(third,10,18);
            foreach(InterfaceLayout layout in System.Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.SetView(1);controller.SelectUnit(third.Id);controller.CloseContextPanel();
                var focus=HexGrid.ToWorld(13,15);controller.SetSurfaceFocus(new Vector3((float)focus.X,0,(float)focus.Y));yield return null;yield return null;
                Vector2 a=controller.SurfaceScreenPoint(tank.X,tank.Y),b=controller.SurfaceScreenPoint(engineer.X,engineer.Y);
                foreach(var delta in new[]{new Vector2(.33f,0),new Vector2(-.33f,0),new Vector2(0,.33f),new Vector2(0,-.33f)})
                {
                    var point=controller.SurfaceScreenPoint(tank.X+delta.x,tank.Y+delta.y);
                    Require(controller.BeginSurfaceSelection(point) && controller.EndSurfaceSelection(point) && controller.SelectedUnitCount==1,
                        "Every side of the occupied hex accepts a click: "+layout);
                }
                var start=Vector2.Min(a,b)-new Vector2(24,35);var end=Vector2.Max(a,b)+new Vector2(24,35);
                Require(controller.BeginSurfaceSelection(start),"Ground selection drag starts in the world: "+layout);controller.ContinueSurfaceSelection(end);
                yield return Capture("selection-rectangle-"+layout+".png");Require(captureFailure==null,"Selection rectangle captured: "+layout);
                Require(controller.SelectedUnitCount==1,"Mouse-down does not erase the selection before a drag completes: "+layout);
                Require(controller.EndSurfaceSelection(end)&&controller.SelectedUnitCount==2,"Rectangle replaces selection with both enclosed vehicles: "+layout);
                controller.SelectUnit(third.Id);controller.CloseContextPanel();controller.BeginSurfaceSelection(start);controller.ContinueSurfaceSelection(end);controller.EndSurfaceSelection(end,true);
                Require(controller.SelectedUnitCount==3,"Shift rectangle adds enclosed vehicles: "+layout);
                if(layout==InterfaceLayout.OrbitalBridge)Require(!controller.IsContextCardOpen,"Layout B keeps selected vehicle commands in the bottom bar without a popup");
                yield return Capture("selection-commands-"+layout+".png");Require(captureFailure==null,"Vehicle commands captured: "+layout);
                controller.CloseContextPanel();var empty=controller.SurfaceScreenPoint(17,18);controller.BeginSurfaceSelection(empty);controller.EndSurfaceSelection(empty);
                Require(controller.SelectedUnitCount==0,"A completed landscape click clears unit focus: "+layout);
                string before=SaveCodec.ToJson(controller.Simulation.State);
                for(int view=0;view<2;view++)
                {
                    controller.SetView(view);controller.CloseContextPanel();yield return null;yield return null;
                    var map=controller.RadarMapRect;Require(map.width>20&&controller.RadarViewport!=null&&controller.RadarViewport.Length==4,"Radar provides a projected viewport in both worlds: "+layout+"/"+view);
                    var prior=view==0?controller.SpaceFocus:controller.SurfaceFocus;
                    Require(controller.BeginRadarDrag(map.center),"Radar viewport accepts dragging: "+layout+"/"+view);
                    controller.ContinueRadarDrag(map.center+new Vector2(12,8));controller.EndRadarDrag();yield return null;
                    var now=view==0?controller.SpaceFocus:controller.SurfaceFocus;
                    Require(now.x>prior.x&&now.z<prior.z,"Radar drag moves the actual camera in matching terrain directions: "+layout+"/"+view);
                    yield return Capture("radar-viewport-"+layout+"-"+view+".png");Require(captureFailure==null,"Radar viewport captured: "+layout+"/"+view);
                }
                Require(before==SaveCodec.ToJson(controller.Simulation.State),"Selection and radar navigation do not mutate the simulation: "+layout);
            }
        }
    }
}
