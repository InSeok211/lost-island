using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        static void TrailGround(Transform root){
            Vector3 end=new Vector3(-330,0,-180),side=Vector3.Cross(Vector3.up,end.normalized);
            var verts=new List<Vector3>();var indices=new List<int>();var colors=new List<Color>();
            // A vertex-coloured, terrain-following shoulder, leaving the walking lane intact.
            for(int sign=-1;sign<=1;sign+=2)for(int i=8;i<175;i++){
                float t=i/190f;Vector3 center=end*t;if((center-new Vector3(-130,0,-80)).sqrMagnitude<30*30)continue;
                for(int band=0;band<4;band++){
                    int start=verts.Count;
                    foreach(var corner in new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)}){
                        float u=(i+corner.x)/190f;float width=4+band*3+corner.y*3;
                        if(band==3&&corner.y>0)width+=Mathf.PerlinNoise(u*40,sign+2)*5;
                        var p=Ground(root,end*u+side*sign*width)+Vector3.up*.06f;verts.Add(p);
                        float blend=(band+corner.y)/4f;colors.Add(Color.Lerp(new Color(.38f,.32f,.22f),new Color(.32f,.37f,.21f),blend));
                    }
                    indices.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3,start+1,start+2,start,start+3,start+2,start+1});
                }
            }
            // Separate earthy strips use standard URP materials; no custom shader dependency.
            var mesh=new Mesh{name="Trail soil shoulders"};mesh.SetVertices(verts);mesh.SetTriangles(indices,0);var normals=new List<Vector3>();foreach(var p in verts)normals.Add(Vector3.up);mesh.SetNormals(normals);mesh.RecalculateBounds();
            var go=new GameObject("Moss and earth shoulders");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Mat("moss soil",new Color(.34f,.36f,.23f));go.AddComponent<AnchorGeneratedMesh>().mesh=mesh;
        }
    }
}
