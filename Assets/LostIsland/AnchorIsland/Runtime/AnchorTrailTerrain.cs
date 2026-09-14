using UnityEngine;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorTrailTerrain : MonoBehaviour {
        Terrain terrain;TerrainData original,copy;TerrainCollider collision;
        public static float Influence(float x,float z){
            var p=new Vector2(x,z);var end=new Vector2(-330,-180);float t=Mathf.Clamp01(Vector2.Dot(p,end)/end.sqrMagnitude);
            float distance=Vector2.Distance(p,end*t);float outer=34+Mathf.PerlinNoise(x*.025f,z*.025f)*15;
            float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(outer-15,outer,distance));
            float start=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.13f,t));
            float finish=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.84f,.99f,t));
            float road=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,8,distance));
            float market=Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,40,Vector2.Distance(p,new Vector2(-130,-80))));
            return edge*start*finish*road*market;
        }
        void Start(){
            terrain=GetComponent<Terrain>();if(!terrain||terrain.terrainData.alphamapLayers<6)return;
            original=terrain.terrainData;copy=Instantiate(original);copy.name=original.name+" - temporary trail blend";
            int width=copy.alphamapWidth,height=copy.alphamapHeight;var map=copy.GetAlphamaps(0,0,width,height);Vector3 origin=terrain.transform.position;
            for(int z=0;z<height;z++)for(int x=0;x<width;x++){
                float wx=origin.x+x/(float)(width-1)*copy.size.x,wz=origin.z+z/(float)(height-1)*copy.size.z;
                float mix=Influence(wx,wz)*.85f;if(mix<=0)continue;
                for(int layer=0;layer<copy.alphamapLayers;layer++)map[z,x,layer]*=1-mix;
                float noise=Mathf.PerlinNoise(wx*.07f,wz*.07f);map[z,x,0]+=mix*(.55f+.25f*noise);map[z,x,5]+=mix*(.45f-.25f*noise);
            }
            copy.SetAlphamaps(0,0,map);terrain.terrainData=copy;collision=GetComponent<TerrainCollider>();if(collision)collision.terrainData=copy;
        }
        void OnDestroy(){if(terrain&&original)terrain.terrainData=original;if(collision&&original)collision.terrainData=original;if(copy)Destroy(copy);}
    }
}
