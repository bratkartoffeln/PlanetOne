using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public enum WorldPointerAction { Default, Move, Attack, Blocked, Context }

    public sealed partial class PrototypeController
    {
        private struct PointerTarget {public long Id;public int Owner;public bool Building;}
        private Vector2? pointerPreview;
        private bool pointerPreviewControl;
        internal void PreviewWorldPointer(Vector2? point,bool control=false){pointerPreview=point;pointerPreviewControl=control;}
        private void OnApplicationFocus(bool focused)
        {if(!focused&&!PrototypeSmokeRunner.IsRequested)Cursor.visible=true;}

        public Rect UnitSelectionScreenRect(long id,bool space)
        {
            Transform view;var views=space?shipViews:unitViews;
            if(!views.TryGetValue(id,out view)||view==null||!view.gameObject.activeInHierarchy)return new Rect();
            var parts=view.GetComponent<UnitSelectionGeometry>();
            if(parts==null)parts=view.gameObject.AddComponent<UnitSelectionGeometry>();
            Bounds bounds;if(!parts.TryBounds(out bounds))return new Rect();
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=-new Vector2(float.MaxValue,float.MaxValue);
            for(int i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var screen=sceneCamera.WorldToScreenPoint(corner);if(screen.z<=0)return new Rect();
                min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);
            }
            var center=(min+max)*.5f;var size=Vector2.Max(max-min+Vector2.one*12,Vector2.one*28);
            return new Rect(center-size*.5f,size);
        }
        private PointerTarget PointerTargetAtScreen(Vector2 point)
        {
            var target=new PointerTarget();float nearest=float.MaxValue;
            if(openPlanetId==0)
            {
                foreach(var ship in simulation.State.Ships)
                {
                    if(!simulation.IsShipVisible(PlayerFactionId,ship))continue;
                    var rect=UnitSelectionScreenRect(ship.Id,true);float distance=Vector2.Distance(rect.center,point);
                    if(rect.width>0&&rect.Contains(point)&&distance<nearest){nearest=distance;target.Id=ship.Id;target.Owner=ship.OwnerId;}
                }
            }
            else
            {
                var planet=simulation.FindPlanet(openPlanetId);
                foreach(var unit in planet.Units)
                {
                    if(unit.Destroyed||!simulation.CanSeeEntity(planet,PlayerFactionId,unit.Id))continue;
                    var rect=UnitSelectionScreenRect(unit.Id,false);float distance=Vector2.Distance(rect.center,point);
                    if(rect.width>0&&rect.Contains(point)&&distance<nearest){nearest=distance;target.Id=unit.Id;target.Owner=unit.OwnerId;}
                }
                if(target.Id==0)
                {
                    var cell=SurfaceCellAtScreen(point);var building=BuildingAt(planet,cell.X,cell.Y);
                    if(building!=null&&!building.Destroyed&&simulation.CanSeeEntity(planet,PlayerFactionId,building.Id))
                    {target.Id=building.Id;target.Owner=GameSimulation.BuildingOwner(planet,building);target.Building=true;}
                }
            }
            return target;
        }
        public WorldPointerAction PointerActionAtScreen(Vector2 point,bool control=false)
        {
            if(!IsWorldPoint(point)||placingRoad||removingRoad||placement.HasValue)return WorldPointerAction.Default;
            if(control)return WorldPointerAction.Context;
            var actors=openPlanetId==0?selectedShips:selectedUnits;if(actors.Count==0)return WorldPointerAction.Default;
            var target=PointerTargetAtScreen(point);
            if(target.Id!=0&&target.Owner!=PlayerFactionId)
            {
                if(!simulation.AtWar(PlayerFactionId,target.Owner))return WorldPointerAction.Blocked;
                bool armed=openPlanetId==0
                    ?simulation.State.Ships.Exists(s=>actors.Contains(s.Id)&&Definitions.SpaceWeapons[(int)s.Kind].Damage>0)
                    :simulation.FindPlanet(openPlanetId).Units.Exists(u=>actors.Contains(u.Id)&&Definitions.Get(u.Kind).Damage>0);
                return armed?WorldPointerAction.Attack:WorldPointerAction.Blocked;
            }
            if(openPlanetId!=0)
            {
                var planet=simulation.FindPlanet(openPlanetId);var cell=SurfaceCellAtScreen(point);
                if(cell.X<0||cell.Y<0||cell.X>=planet.Width||cell.Y>=planet.Height)return WorldPointerAction.Blocked;
            }
            return WorldPointerAction.Move;
        }
        public bool IssueDefaultOrderAtScreen(Vector2 point)
        {
            var action=PointerActionAtScreen(point);
            if(action==WorldPointerAction.Default||action==WorldPointerAction.Context)return false;
            if(action==WorldPointerAction.Blocked){Notify("Hier ist kein direkter Auftrag möglich. Strg + Rechtsklick öffnet weitere Aktionen.");return false;}
            if(action==WorldPointerAction.Move)
            {
                if(openPlanetId==0)return RequestShipMoveAtScreen(point);
                var cell=SurfaceCellAtScreen(point);bool moved=false;
                foreach(long id in selectedUnits)moved=RequestUnitOrder(id,CommandKind.MoveUnit,cell.X,cell.Y)||moved;
                return moved;
            }
            var target=PointerTargetAtScreen(point);bool accepted=false;
            if(openPlanetId==0)
            {
                foreach(long id in selectedShips)
                {
                    var ship=simulation.State.Ships.Find(s=>s.Id==id);
                    if(ship!=null&&Definitions.SpaceWeapons[(int)ship.Kind].Damage>0)accepted=RequestInvasionOrder(CommandKind.AttackShip,id,target.Id)||accepted;
                }
            }
            else
            {
                var cell=SurfaceCellAtScreen(point);var planet=simulation.FindPlanet(openPlanetId);
                foreach(long id in selectedUnits)
                {
                    var unit=planet.Units.Find(u=>u.Id==id);
                    if(unit!=null&&Definitions.Get(unit.Kind).Damage>0)accepted=RequestUnitOrder(id,CommandKind.AttackTarget,cell.X,cell.Y,target.Id)||accepted;
                }
            }
            return accepted;
        }
        private void DrawUnitSelectionFrames(float scale)
        {
            foreach(long id in openPlanetId==0?selectedShips:selectedUnits)
            {
                var rect=UnitSelectionScreenRect(id,openPlanetId==0);if(rect.width==0||!sceneCamera.pixelRect.Overlaps(rect))continue;
                DrawTargetBrackets(new Rect(rect.x/scale,(Screen.height-rect.yMax)/scale,rect.width/scale,rect.height/scale),Cyan);
            }
        }
        private void DrawWorldPointer(float scale)
        {
            bool preview=pointerPreview.HasValue;
            if(!preview&&!Application.isFocused)return;
            var point=preview?pointerPreview.Value:(Vector2)Input.mousePosition;
            bool control=preview?pointerPreviewControl:Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
            var action=PointerActionAtScreen(point,control);
            // A hidden automated player never changes the working desktop's operating-system pointer.
            if(!preview&&!PrototypeSmokeRunner.IsRequested)Cursor.visible=action==WorldPointerAction.Default;
            if(action==WorldPointerAction.Default)return;
            var center=new Vector2(point.x/scale,(Screen.height-point.y)/scale);float r=10/scale;
            Color color=action==WorldPointerAction.Attack?Danger:action==WorldPointerAction.Blocked?Amber:Cyan;
            if(action==WorldPointerAction.Context)
            {
                for(int i=0;i<3;i++)Fill(new Rect(center.x-r,center.y-r+i*6/scale,r*2,2/scale),color);
                return;
            }
            for(int i=0;i<28;i++)
            {
                float a=i*Mathf.PI*2/28,b=(i+1)*Mathf.PI*2/28;
                DrawMapLine(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,color,1.5f/scale);
            }
            if(action==WorldPointerAction.Blocked){DrawMapLine(center+new Vector2(-r,-r)*.7f,center+new Vector2(r,r)*.7f,color,2/scale);return;}
            for(int i=0;i<4;i++)
            {
                var direction=new Vector2(Mathf.Cos(i*Mathf.PI*.5f),Mathf.Sin(i*Mathf.PI*.5f));
                DrawMapLine(center+direction*(action==WorldPointerAction.Attack?r*.45f:r),center+direction*(r+5/scale),color,2/scale);
            }
            Fill(new Rect(center.x-1/scale,center.y-1/scale,2/scale,2/scale),color);
        }
    }
    public sealed class UnitSelectionGeometry:MonoBehaviour
    {
        private Renderer[] parts;
        private void Awake(){parts=GetComponentsInChildren<Renderer>();}
        internal bool TryBounds(out Bounds bounds)
        {
            bounds=new Bounds();bool found=false;
            foreach(var part in parts)
            {
                if(part==null||part is LineRenderer||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                if(!found){bounds=part.bounds;found=true;}else bounds.Encapsulate(part.bounds);
            }
            return found;
        }
    }
}
