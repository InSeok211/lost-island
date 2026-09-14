using System.Collections.Generic;
using UnityEngine;
namespace LostIsland.AnchorIsland {
    public static partial class AnchorSettlementDress {
        // A single roof mesh with individually shaped shingles, rather than hundreds of objects.
        static void ShingleRoof(Transform root){
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();var uv=new List<Vector2>();
            for(int side=-1;side<=1;side+=2)for(int row=0;row<8;row++)for(int column=0;column<18;column++){
                float x0=row*.73f,x1=x0+.79f,z0=-5.2f+column*.58f,z1=z0+.565f;
                float jitter=Mathf.PerlinNoise(row*3.71f,column*4.13f+side*11)*.008f;
                float y0=7.9f-x0*.466f+row*.055f+jitter,y1=7.9f-x1*.466f+row*.055f+jitter;
                int n=vertices.Count;vertices.Add(new Vector3(side*x0,y0,z0));vertices.Add(new Vector3(side*x0,y0,z1));vertices.Add(new Vector3(side*x1,y1,z1));vertices.Add(new Vector3(side*x1,y1,z0));
                if(side>0)triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});else triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
                // Vertex colors are retained for art tooling; UV treatment supplies runtime variation.
                for(int i=0;i<4;i++)colors.Add(Color.white);
            }
            var mesh=new Mesh{name="Overlapping cedar roof shingles"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var roof=new GameObject("Hand-laid shingle roof");roof.transform.SetParent(root,false);roof.AddComponent<MeshFilter>().sharedMesh=mesh;roof.AddComponent<MeshRenderer>().sharedMaterial=Mat("roof cedar wood",new Color(.32f,.29f,.22f));roof.AddComponent<AnchorOwnedMesh>();
            foreach(int side in new[]{-1,1}){
                Part(root,"Eave fascia",new Vector3(side*5.75f,5.48f,0),new Vector3(.22f,.42f,10.6f),Wood,PrimitiveType.Cube,false);
                foreach(float z in new[]{-5.2f,5.2f}){var beam=Part(root,"Gable verge",new Vector3(side*2.88f,6.6f,z),new Vector3(6.4f,.22f,.22f),Wood,PrimitiveType.Cube,false);beam.transform.localRotation=Quaternion.Euler(0,0,-side*22);}
            }
            Part(root,"Ridge cap",new Vector3(0,7.97f,0),new Vector3(.35f,.2f,10.7f),Wood,PrimitiveType.Cube,false);
        }
        static void ArchitectureFinish(Transform root){
            ShingleRoof(root);
            var gable=new Mesh{name="Closed timber gables"};gable.vertices=new[]{new Vector3(-5,5.3f,-4),new Vector3(0,7.5f,-4),new Vector3(5,5.3f,-4),new Vector3(-5,5.3f,4),new Vector3(0,7.5f,4),new Vector3(5,5.3f,4)};gable.triangles=new[]{0,1,2,3,5,4};gable.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.zero,Vector2.up,Vector2.right};gable.RecalculateNormals();gable.RecalculateBounds();var face=new GameObject("Gable infill");face.transform.SetParent(root,false);face.AddComponent<MeshFilter>().sharedMesh=gable;face.AddComponent<MeshRenderer>().sharedMaterial=Plaster;face.AddComponent<AnchorOwnedMesh>();
            var stone=Mat("foundation stone",new Color(.46f,.46f,.38f));
            foreach(int side in new[]{-1,1})Part(root,"Upper gable timber infill",new Vector3(0,5.55f,side*4),new Vector3(10,.5f,.12f),Wood,PrimitiveType.Cube,false);
            for(int side=-1;side<=1;side+=2){
                for(int i=0;i<9;i++)Part(root,"Foundation masonry",new Vector3(-4.8f+i*1.2f,.25f,side*4.42f),new Vector3(1.14f,.5f,.25f),stone,PrimitiveType.Cube,false);
                for(int z=-3;z<=3;z+=3)Part(root,"Corner timber",new Vector3(side*5.06f,2.85f,z),new Vector3(.22f,5.2f,.24f),Wood,PrimitiveType.Cube,false);
                Part(root,"Wall sill",new Vector3(side*5.07f,.8f,0),new Vector3(.2f,.28f,8.1f),Wood,PrimitiveType.Cube,false);
                foreach(int edge in new[]{-1,1})Part(root,"Front frame",new Vector3(side*4.85f,2.85f,edge*4.12f),new Vector3(.25f,5.2f,.24f),Wood,PrimitiveType.Cube,false);
                Part(root,"Window sill",new Vector3(side*3,1.82f,-4.3f),new Vector3(2.35f,.18f,.55f),Wood,PrimitiveType.Cube,false);
                Part(root,"Glass mullion",new Vector3(side*3,2.8f,-4.3f),new Vector3(.09f,1.55f,.12f),Cream,PrimitiveType.Cube,false);
                Part(root,"Glass crosspiece",new Vector3(side*3,2.8f,-4.3f),new Vector3(1.6f,.09f,.12f),Cream,PrimitiveType.Cube,false);
                for(int slat=0;slat<7;slat++)Part(root,"Open shutter slat",new Vector3(side*3+side*1.22f,2.1f+slat*.23f,-4.22f),new Vector3(.55f,.19f,.15f),Teal,PrimitiveType.Cube,false);
                Part(root,"Door jamb",new Vector3(side*.88f,1.75f,-4.28f),new Vector3(.18f,3,.28f),Wood,PrimitiveType.Cube,false);
            }
            Part(root,"Door lintel",new Vector3(0,3.3f,-4.28f),new Vector3(2,.24f,.3f),Wood,PrimitiveType.Cube,false);
            for(int i=0;i<5;i++)Part(root,"Door board",new Vector3(-.6f+i*.3f,1.7f,-4.21f),new Vector3(.27f,2.65f,.07f),Mat("door wood",new Color(.41f,.3f,.18f)),PrimitiveType.Cube,false);
            Part(root,"Door latch",new Vector3(.48f,1.7f,-4.31f),new Vector3(.08f,.3f,.12f),Teal,PrimitiveType.Cube,false);
            // Decorative pieces deliberately preserve the tested entrance and walking colliders.
        }
    }
    public sealed class AnchorOwnedMesh:MonoBehaviour {
        void OnDestroy(){var filter=GetComponent<MeshFilter>();if(filter&&filter.sharedMesh)Destroy(filter.sharedMesh);}
    }
}
