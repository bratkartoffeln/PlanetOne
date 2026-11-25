using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Mesh hexMesh;

        public GridCell SurfaceCellAtScreen(Vector2 point)
        {
            var ray = sceneCamera.ScreenPointToRay(point); RaycastHit hit;
            if(reliefCollider==null || !reliefCollider.Raycast(ray,out hit,300))return new GridCell(-1,-1);
            return HexGrid.FromWorld(hit.point.x,hit.point.z);
        }

        public Vector2 SurfaceScreenPoint(double x, double y)
        { return sceneCamera.WorldToScreenPoint(SurfacePosition(x, 0, y)); }

        public bool IsSurfacePointVisible(double x,double y,float height=0)
        {
            var position=SurfacePosition(x,height,y);var screen=sceneCamera.WorldToScreenPoint(position);
            if(screen.z<=0 || !sceneCamera.pixelRect.Contains(screen))return false;
            var ray=sceneCamera.ScreenPointToRay(screen);RaycastHit hit;
            if(reliefCollider==null)return false;
            bool intersects=reliefCollider.Raycast(ray,out hit,300);
            float distance=Vector3.Dot(position-ray.origin,ray.direction);
            // A ground point is visible only when it is the front surface. A back-facing
            // ridge may produce a later hit and must not be classified as visible.
            return height==0?intersects && Mathf.Abs(hit.distance-distance)<0.02f:!intersects || hit.distance+0.02f>=distance;
        }

        public float SurfaceElevation(double x,double y){return SurfacePosition(x,0,y).y;}

        // Batch smoke tests render the real scene without a window or desktop input.
        // IMGUI is not part of Camera.Render; this is deliberately a world-render proof.
        public void CaptureWorld(string path)
        {
            var target = RenderTexture.GetTemporary(1440, 900, 24);
            var previous = RenderTexture.active; var oldTarget = sceneCamera.targetTexture;
            var oldRect = sceneCamera.rect; var oldAspect = sceneCamera.aspect;
            var pixels = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            try
            {
                sceneCamera.targetTexture = target; sceneCamera.rect = new Rect(0, 0, 1, 1); sceneCamera.aspect = 1440f / 900;
                sceneCamera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                sceneCamera.targetTexture = oldTarget; sceneCamera.rect = oldRect; sceneCamera.aspect = oldAspect;
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(pixels);
            }
        }

        private Vector3 SurfacePosition(double x, float height, double y)
        {
            var point = HexGrid.ToWorld(x, y);
            return new Vector3((float)point.X, height+ReliefHeight((float)point.X,(float)point.Y), (float)point.Y);
        }

        private static Vector3 HexCorner(int corner, float scale)
        {
            float angle = (30 + corner * 60) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (float)HexGrid.Radius * scale;
        }

        private Transform MakeHex(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            if (hexMesh == null)
            {
                hexMesh = new Mesh { name = "Wabenprisma" };
                var vertices = new Vector3[14];
                vertices[0] = Vector3.up * 0.5f; vertices[7] = Vector3.down * 0.5f;
                for (int i = 0; i < 6; i++)
                { vertices[i + 1] = HexCorner(i, 1) + vertices[0]; vertices[i + 8] = HexCorner(i, 1) + vertices[7]; }
                var triangles = new int[72]; int index = 0;
                for (int i = 0; i < 6; i++)
                {
                    int a = i + 1, b = (i + 1) % 6 + 1;
                    foreach (int v in new[] { 0,b,a, 7,a+7,b+7, a,b,b+7, a,b+7,a+7 }) triangles[index++] = v;
                }
                hexMesh.vertices = vertices; hexMesh.triangles = triangles; hexMesh.RecalculateNormals(); hexMesh.RecalculateBounds();
            }
            var item = new GameObject(name);
            item.transform.SetParent(parent, false); item.transform.localPosition = position; item.transform.localScale = scale;
            item.AddComponent<MeshFilter>().sharedMesh = hexMesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = ArtMaterial(name,color,false);
            return item.transform;
        }

        private Transform MakeHexOutline(string name, Transform parent, Color color, float width)
        {
            var line = MakeLine(name, parent, color, width, true); line.positionCount = 6;
            for (int i = 0; i < 6; i++) line.SetPosition(i, HexCorner(i, 0.96f));
            return line.transform;
        }
    }
}
