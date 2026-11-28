using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private sealed class OrderArrow
        {
            public SimCommand Command;
            public long Tick, Sequence;
            public bool Attack, Space;
        }
        private readonly Dictionary<long,OrderArrow> orderArrows=new Dictionary<long,OrderArrow>();
        private readonly List<long> expiredOrderArrows=new List<long>();
        private GameSimulation arrowSimulation;
        public int ActiveOrderArrowCount {get{return orderArrows.Count;}}
        internal int VisibleOrderArrowCount {get;private set;}

        private void TrackOrderArrow(SimCommand command)
        {
            if(arrowSimulation!=simulation){orderArrows.Clear();arrowSimulation=simulation;}
            bool space=command.Kind==CommandKind.MoveShipPosition||command.Kind==CommandKind.AttackShip;
            bool ground=command.Kind==CommandKind.MoveUnit||command.Kind==CommandKind.AttackTarget||command.Kind==CommandKind.AttackPosition;
            if(command.ActorId==0)return;
            orderArrows.Remove(command.ActorId);
            if(!space&&!ground)return;
            orderArrows[command.ActorId]=new OrderArrow{Command=command,Space=space,
                Attack=command.Kind!=CommandKind.MoveUnit&&command.Kind!=CommandKind.MoveShipPosition,
                Tick=simulation.State.Tick,Sequence=simulation.CombatPresentationSequence};
        }

        private void DrawOrderArrows(float scale)
        {
            VisibleOrderArrowCount=0;
            if(arrowSimulation!=simulation){orderArrows.Clear();arrowSimulation=simulation;return;}
            expiredOrderArrows.Clear();
            foreach(var entry in orderArrows)
            {
                var arrow=entry.Value;var command=arrow.Command;Vector3 start,end;bool active=true,visible=true;
                var result=simulation.State.CommandResults.Find(r=>r.CommandId==command.Id);
                if(result!=null&&!result.Success){expiredOrderArrows.Add(entry.Key);continue;}
                if(arrow.Space)
                {
                    var ship=simulation.State.Ships.Find(s=>s.Id==entry.Key&&!s.Destroyed&&!s.Consumed);
                    if(ship==null){expiredOrderArrows.Add(entry.Key);continue;}
                    active=arrow.Attack?ship.Order==ShipOrder.Attack&&ship.CombatTargetId==command.TargetId:ship.Order==ShipOrder.MovePosition;
                    start=SpacePosition(ship.X,ship.Y,ShipDisplayHeight);end=SpacePosition(command.X,command.Y,ShipDisplayHeight);
                    if(arrow.Attack)
                    {
                        var target=simulation.State.Ships.Find(s=>s.Id==command.TargetId&&!s.Destroyed&&!s.Consumed);
                        active&=target!=null;
                        visible=target!=null&&simulation.IsShipVisible(PlayerFactionId,target);
                        if(visible)end=SpacePosition(target.X,target.Y,ShipDisplayHeight);
                    }
                }
                else
                {
                    var planet=simulation.FindPlanet(command.PlanetId);
                    var unit=planet==null?null:planet.Units.Find(u=>u.Id==entry.Key&&!u.Destroyed);
                    if(unit==null){expiredOrderArrows.Add(entry.Key);continue;}
                    active=arrow.Attack?(unit.Order==UnitOrder.Attack&&unit.TargetId==command.TargetId||unit.Order==UnitOrder.Bombard):unit.Order==UnitOrder.Move;
                    start=SurfacePosition(unit.X,.3f,unit.Y);end=SurfacePosition(command.X,.3f,command.Y);
                    if(arrow.Attack&&command.TargetId!=0)
                    {
                        var target=planet.Units.Find(u=>u.Id==command.TargetId&&!u.Destroyed);
                        var building=planet.Buildings.Find(b=>b.Id==command.TargetId&&!b.Destroyed);
                        active&=target!=null||building!=null;
                        visible=simulation.CanSeeEntity(planet,PlayerFactionId,command.TargetId);
                        if(visible&&target!=null)end=SurfacePosition(target.X,.3f,target.Y);
                        else if(visible&&building!=null)end=SurfacePosition(building.X,.3f,building.Y);
                    }
                }
                foreach(var fact in simulation.CombatPresentationEvents)
                    if(arrow.Attack&&fact.Sequence>arrow.Sequence&&fact.SourceId==entry.Key&&
                        fact.Kind!=CombatVisualKind.Explosion&&(command.TargetId==0||fact.TargetId==command.TargetId))active=false;
                // Keep the immediate click feedback while its validated command is queued.
                if(!active&&simulation.State.Tick>arrow.Tick){expiredOrderArrows.Add(entry.Key);continue;}
                if(!visible||arrow.Space!=(openPlanetId==0)||!arrow.Space&&command.PlanetId!=openPlanetId)continue;
                if(!(arrow.Space?selectedShips:selectedUnits).Contains(entry.Key))continue;
                VisibleOrderArrowCount++;
                Transform view;
                if((arrow.Space?shipViews:unitViews).TryGetValue(entry.Key,out view)&&view!=null)start=view.position+Vector3.up*.15f;
                DrawBallisticOrderArrow(start,end,arrow.Attack?new Color(1,.16f,.12f,.3f):new Color(.2f,1,.38f,.28f),scale);
            }
            foreach(long id in expiredOrderArrows)orderArrows.Remove(id);
        }

        private void DrawBallisticOrderArrow(Vector3 from,Vector3 to,Color color,float scale)
        {
            float height=Mathf.Clamp(Vector3.Distance(from,to)*.22f,.65f,8);
            Vector2 previous=Vector2.zero,last=Vector2.zero;bool previousVisible=false;
            for(int i=0;i<=40;i++)
            {
                float t=i/40f;var world=Vector3.Lerp(from,to,t)+Vector3.up*(4*height*t*(1-t));
                var screen=sceneCamera.WorldToScreenPoint(world);var point=new Vector2(screen.x/scale,(Screen.height-screen.y)/scale);
                bool visible=screen.z>0&&IsWorldPoint(screen);
                if(i>0&&visible&&previousVisible)DrawMapLine(previous,point,color,3/scale);
                last=previous;previous=point;previousVisible=visible;
            }
            if(!previousVisible)return;
            var direction=(previous-last).normalized;var side=new Vector2(-direction.y,direction.x);
            DrawMapLine(previous,previous-direction*14/scale+side*7/scale,color,4/scale);
            DrawMapLine(previous,previous-direction*14/scale-side*7/scale,color,4/scale);
        }
    }
}
