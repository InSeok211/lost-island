using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void NatureCorridor(Transform parent){
            var root=new GameObject("Art study - research trail").transform;root.SetParent(parent,false);
            // Ground colour now comes from blended Terrain layers, not overlay strips.
            var random=new System.Random(9042);Vector3 end=new Vector3(-330,0,-180),forward=end.normalized,side=Vector3.Cross(Vector3.up,forward);
            var greens=new[]{Mat("fern olive",new Color(.29f,.36f,.15f)),Mat("fern deep",new Color(.16f,.28f,.15f)),Mat("fern light",new Color(.4f,.46f,.23f))};
            var bark=Mat("weathered bark",new Color(.24f,.2f,.15f));var rock=Mat("trail rock",new Color(.4f,.42f,.35f));
            var vertices=new List<Vector3>();var triangles=new List<int>();
            // Leaflets are pointed, double-sided geometry, not spherical canopies.
            for(int i=0;i<170;i++){
                float t=.08f+(float)random.NextDouble()*.83f;float lateral=(i%2==0?1:-1)*(7+(float)random.NextDouble()*22);
                Vector3 p=end*t+side*lateral;
                if((p-new Vector3(-130,0,-80)).sqrMagnitude<32*32)continue;
                p=Ground(root,p);float size=.8f+(float)random.NextDouble()*1.7f;
                for(int frond=0;frond<7;frond++){
                    float angle=frond*Mathf.PI*2/7+(float)random.NextDouble();Vector3 dir=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));Vector3 cross=Vector3.Cross(Vector3.up,dir);
                    for(int leaf=1;leaf<6;leaf++){
                        float u=leaf/6f;Vector3 center=p+dir*(u*size)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*size*.6f+.15f);
                        float breadth=(1-u)*size*.4f;
                        foreach(int sign in new[]{-1,1}){int start=vertices.Count;vertices.Add(center-dir*.15f);vertices.Add(center+cross*breadth*sign+dir*.18f);vertices.Add(center+dir*.24f);triangles.AddRange(new[]{start,start+1,start+2,start+2,start+1,start});}
                    }
                }
                if(i%13==0){var boulder=Part(root,"Weathered outcrop",p+Vector3.up*.35f,new Vector3(size*1.7f,size*.8f,size),rock,PrimitiveType.Sphere,false);boulder.transform.localRotation=Quaternion.Euler(12,i*31,18);}
                if(i%29==0){var log=Part(root,"Fallen trunk",p+Vector3.up*.45f,new Vector3(.7f,3.8f,.7f),bark,PrimitiveType.Cylinder,false);log.transform.localRotation=Quaternion.Euler(85,i*17,0);}
            }
            var mesh=new Mesh{name="Research trail fern leaflets"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);var normals=new List<Vector3>();foreach(var vertex in vertices)normals.Add(Vector3.up);mesh.SetNormals(normals);mesh.RecalculateBounds();
            var leaves=new GameObject("Fern understory");leaves.transform.SetParent(root,false);leaves.AddComponent<MeshFilter>().sharedMesh=mesh;leaves.AddComponent<MeshRenderer>().sharedMaterial=greens[0];leaves.AddComponent<AnchorGeneratedMesh>().mesh=mesh;
            // Broken gravel margins retain the existing collision-free walking lane.
            for(int i=0;i<220;i++){
                float t=.07f+(float)random.NextDouble()*.85f;Vector3 p=end*t+side*((i%2==0?1:-1)*(3.8f+(float)random.NextDouble()*1.6f));p=Ground(root,p);
                Part(root,"Trail edge gravel",p+Vector3.up*.13f,new Vector3(.3f+(float)random.NextDouble()*.8f,.22f,.6f),rock,PrimitiveType.Sphere,false);
            }
        }
    }
    public sealed class AnchorGeneratedMesh : MonoBehaviour {public Mesh mesh;void OnDestroy(){if(mesh)Destroy(mesh);}}
}
