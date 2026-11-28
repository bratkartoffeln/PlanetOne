using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Rect radarOuterRect,radarMapRect;
        private float radarExtent;
        private bool radarDragging;
        private Vector2 radarDragStart;
        private Vector3 radarFocusStart;
        internal Rect RadarMapRect {get{return radarMapRect;}}
        internal Vector2[] RadarViewport {get;private set;}
        private Vector2 WorldOnRadar(Vector3 world)
        {
            if(openPlanetId==0)return radarMapRect.center+new Vector2(world.x,-world.z)/(radarExtent*SpaceDistanceScale)*new Vector2(radarMapRect.width,radarMapRect.height)*.42f;
            var p=simulation.FindPlanet(openPlanetId);
            return new Vector2(radarMapRect.x+world.x/p.Width*radarMapRect.width,radarMapRect.yMax-world.z/(p.Height*(float)PlanetOne.Core.HexGrid.RowHeight)*radarMapRect.height);
        }
        private Vector3 RadarDelta(Vector2 delta)
        {
            if(openPlanetId==0)return new Vector3(delta.x/radarMapRect.width,0,-delta.y/radarMapRect.height)*(radarExtent*SpaceDistanceScale/.42f);
            var p=simulation.FindPlanet(openPlanetId);return new Vector3(delta.x/radarMapRect.width*p.Width,0,-delta.y/radarMapRect.height*p.Height*(float)PlanetOne.Core.HexGrid.RowHeight);
        }
        internal bool BeginRadarDrag(Vector2 guiPoint)
        {
            if(!radarMapRect.Contains(guiPoint)||settingsOpen||menuOpen||viewFlightActive)return false;
            radarDragging=true;radarDragStart=guiPoint;radarFocusStart=openPlanetId==0?spaceFocus:surfaceFocus;return true;
        }
        internal void ContinueRadarDrag(Vector2 guiPoint)
        {
            if(!radarDragging)return;var focus=radarFocusStart+RadarDelta(guiPoint-radarDragStart);
            if(openPlanetId==0)SetSpaceFocus(focus);else SetSurfaceFocus(focus);UpdateCamera();
        }
        internal void EndRadarDrag(){radarDragging=false;}
        private void DrawRadarViewport(Rect outer,Rect map,float extent)
        {
            radarOuterRect=outer;radarMapRect=map;radarExtent=extent;
            var e=Event.current;
            if(e.button==0&&e.type==EventType.MouseDown&&BeginRadarDrag(e.mousePosition))e.Use();
            if(radarDragging&&e.type==EventType.MouseDrag){ContinueRadarDrag(e.mousePosition);e.Use();}
            if(radarDragging&&e.button==0&&e.type==EventType.MouseUp){ContinueRadarDrag(e.mousePosition);EndRadarDrag();e.Use();}
            var viewport=WorldViewport;var corners=new[]{new Vector2(viewport.xMin,viewport.yMin),new Vector2(viewport.xMax,viewport.yMin),new Vector2(viewport.xMax,viewport.yMax),new Vector2(viewport.xMin,viewport.yMax)};
            var plane=new Plane(Vector3.up,Vector3.zero);var points=new Vector2[4];
            for(int i=0;i<4;i++)
            {
                var ray=sceneCamera.ScreenPointToRay(corners[i]);float distance;
                points[i]=WorldOnRadar(plane.Raycast(ray,out distance)?ray.GetPoint(distance):(openPlanetId==0?spaceFocus:surfaceFocus));
            }
            RadarViewport=points;
            GUI.BeginGroup(map);
            for(int i=0;i<4;i++)DrawMapLine(points[i]-map.position,points[(i+1)%4]-map.position,new Color(.85f,1,1,.9f),1.5f);
            GUI.EndGroup();
        }
    }
}
