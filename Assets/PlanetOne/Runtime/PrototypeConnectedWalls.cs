using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<long,int> wallShapes=new Dictionary<long,int>();
        private static bool IsFortification(BuildingKind kind){return kind==BuildingKind.Wall||kind==BuildingKind.Gate;}
        private int WallShape(PlanetState planet,int x,int y,int owner)
        {
            int shape=0;
            for(int i=0;i<6;i++)
            {
                var n=HexGrid.Neighbor(x,y,i);
                if(planet.Buildings.Exists(b=>!b.Destroyed&&IsFortification(b.Kind)&&b.X==n.X&&b.Y==n.Y&&
                    GameSimulation.BuildingOwner(planet,b)==owner))shape|=1<<i;
            }
            return shape;
        }
        private float WallGround(int x,int y)
        {
            var at=SurfacePosition(x,0,y);float ground=at.y;
            for(int i=0;i<6;i++){var corner=at+HexCorner(i,.89f);ground=Mathf.Max(ground,ReliefHeight(corner.x,corner.z));}
            return ground+.015f;
        }
        private void BuildConnectedWall(Transform root,BuildingKind kind,int x,int y,int owner)
        {
            var planet=simulation.FindPlanet(openPlanetId);int shape=WallShape(planet,x,y,owner);
            var center=SurfacePosition(x,0,y);center.y=WallGround(x,y);
            var ends=new List<Vector3>();
            for(int i=0;i<6;i++)if((shape&(1<<i))!=0)
            {
                var n=HexGrid.Neighbor(x,y,i);var other=SurfacePosition(n.X,0,n.Y);other.y=WallGround(n.X,n.Y);
                ends.Add((other-center)*.5f);
            }
            var axis=ends.Count>0?new Vector3(ends[0].x,0,ends[0].z).normalized:Vector3.right;
            if(ends.Count==2)axis=Vector3.ProjectOnPlane(ends[0]-ends[1],Vector3.up).normalized;
            if(ends.Count==0){ends.Add(axis*.42f);ends.Add(-axis*.42f);}
            if(kind==BuildingKind.Wall)
            {
                Part(root,"Mauerpfeiler",new Vector3(0,.25f,0),new Vector3(.15f,.5f,.15f),Steel);
                Part(root,"Betriebsanzeige",new Vector3(0,.505f,0),new Vector3(.05f,.015f,.05f),Amber);
                foreach(var end in ends)WallArm(root,Vector3.zero,end);
            }
            else
            {
                var gate=ColonyGroup(root,"Ausgerichtetes Tor",Vector3.zero);
                gate.localRotation=Quaternion.FromToRotation(Vector3.right,axis);ColonyGate(gate);
                foreach(var end in ends)
                {
                    var jamb=axis*(Vector3.Dot(end,axis)>=0?.32f:-.32f);
                    WallArm(root,jamb,end);
                }
            }
        }
        private void WallArm(Transform root,Vector3 start,Vector3 end)
        {
            var delta=end-start;if(delta.sqrMagnitude<.00001f)return;
            var part=Part(root,"Verbundene Mauer",(start+end)*.5f+Vector3.up*.23f,new Vector3(.11f,.44f,delta.magnitude+.012f),Hull);
            part.localRotation=Quaternion.LookRotation(delta);
            var cap=Part(root,"Mauerabschluss",(start+end)*.5f+Vector3.up*.46f,new Vector3(.14f,.04f,delta.magnitude+.015f),Trim);
            cap.localRotation=Quaternion.LookRotation(delta);
        }
    }
}
