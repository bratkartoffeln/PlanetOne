using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Camera spaceBackdropCamera;
        private Mesh starfieldMesh;
        private Material starfieldMaterial;
        private Vector3[] backgroundStarPositions;
        internal Camera SpaceBackdropCamera {get{return spaceBackdropCamera;}}
        internal Vector3[] BackgroundStarPositions {get{return backgroundStarPositions;}}

        private void BuildSpaceBackdrop()
        {
            // The universe stays fixed in world space. Only the observer moves and turns.
            nebulaView=MakePrimitive(PrimitiveType.Sphere,"Sternennebel",spaceRoot,Vector3.zero,Vector3.one*44000,Color.white);
            nebulaView.gameObject.layer=29;
            var skyRenderer=nebulaView.GetComponent<Renderer>();skyRenderer.shadowCastingMode=ShadowCastingMode.Off;skyRenderer.receiveShadows=false;
            if(starfieldMesh==null)CreateStarfieldMesh();
            var stars=new GameObject("Sterne in unterschiedlichen Entfernungen");stars.transform.SetParent(spaceRoot,false);stars.layer=29;
            stars.AddComponent<MeshFilter>().sharedMesh=starfieldMesh;
            var renderer=stars.AddComponent<MeshRenderer>();renderer.sharedMaterial=starfieldMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var cameraObject=new GameObject("Perspektivische Hintergrundkamera");cameraObject.transform.SetParent(spaceRoot,false);
            spaceBackdropCamera=cameraObject.AddComponent<Camera>();spaceBackdropCamera.depth=sceneCamera.depth-1;
            spaceBackdropCamera.cullingMask=1<<29;spaceBackdropCamera.clearFlags=CameraClearFlags.SolidColor;
            spaceBackdropCamera.backgroundColor=new Color(0.002f,0.004f,0.009f);
            spaceBackdropCamera.nearClipPlane=1;spaceBackdropCamera.farClipPlane=50000;
            spaceBackdropCamera.fieldOfView=55;spaceBackdropCamera.allowHDR=false;spaceBackdropCamera.allowMSAA=true;
        }

        private void CreateStarfieldMesh()
        {
            const int count=12000;
            var random=new System.Random(831);
            backgroundStarPositions=new Vector3[count];
            var vertices=new Vector3[count*4];var uv=new Vector2[count*4];var sizes=new Vector2[count*4];var colors=new Color[count*4];var indices=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float height=(float)random.NextDouble()*2-1,azimuth=(float)random.NextDouble()*Mathf.PI*2;
                float radius=900+17100*(i+(float)random.NextDouble())/count;
                float flat=Mathf.Sqrt(1-height*height);
                var position=new Vector3(flat*Mathf.Cos(azimuth),height,flat*Mathf.Sin(azimuth))*radius;
                backgroundStarPositions[i]=position;
                float size=radius*Mathf.Lerp(0.00045f,0.0015f,Mathf.Pow((float)random.NextDouble(),3));
                float temperature=(float)random.NextDouble();
                Color color=Color.Lerp(new Color(0.6f,0.77f,1),new Color(1,0.87f,0.67f),temperature);
                color*=Mathf.Lerp(0.6f,1.5f,(float)random.NextDouble());color.a=1;
                for(int corner=0;corner<4;corner++)
                {int index=i*4+corner;vertices[index]=position;uv[index]=new Vector2(corner==1 || corner==2?1:0,corner>=2?1:0);sizes[index]=new Vector2(size,0);colors[index]=color;}
                int v=i*4,t=i*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;
            }
            starfieldMesh=new Mesh{name="Tiefengestaffelter Sternenhimmel"};starfieldMesh.vertices=vertices;starfieldMesh.uv=uv;starfieldMesh.uv2=sizes;starfieldMesh.colors=colors;starfieldMesh.triangles=indices;
            starfieldMesh.bounds=new Bounds(Vector3.zero,Vector3.one*37000);
            starfieldMaterial=new Material(Resources.Load<Shader>("BackgroundStars"));
        }

        private void UpdateSpaceBackdropCamera()
        {
            sceneCamera.clearFlags=openPlanetId==0?CameraClearFlags.Depth:CameraClearFlags.SolidColor;
            if(spaceBackdropCamera==null)return;
            spaceBackdropCamera.enabled=openPlanetId==0;
            spaceBackdropCamera.transform.SetPositionAndRotation(sceneCamera.transform.position,sceneCamera.transform.rotation);
            spaceBackdropCamera.pixelRect=sceneCamera.pixelRect;
        }

        internal static Quaternion PlanetAxialRotation(int id,double seconds)
        {
            double period=9000+id*1200;
            float angle=id*37+(float)((seconds%period)*360/period);
            return Quaternion.Euler(0,0,id==1?12:-8)*Quaternion.Euler(0,angle,0);
        }
    }
}
