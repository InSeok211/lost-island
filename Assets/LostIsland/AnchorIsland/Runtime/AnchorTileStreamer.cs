using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LostIsland.AnchorIsland {
    public sealed class AnchorTileStreamer : MonoBehaviour {
        public Transform target; public bool busy;
        public Vector2Int Tile(Vector3 p)=>new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x+2000)/1000),0,3),Mathf.Clamp(Mathf.FloorToInt((p.z+2000)/1000),0,3));
        public bool Ready(Vector3 p){var t=Tile(p);return SceneManager.GetSceneByName($"AnchorIsland_Tile_{t.x}_{t.y}").isLoaded;}
        public int Loaded {get{int n=0;for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).name.StartsWith("AnchorIsland_Tile_"))n++;return n;}}
        IEnumerator Start(){
            while(target){
                var center=Tile(target.position);var wanted=new HashSet<string>();var order=new List<Vector2Int>();
                for(int z=center.y-1;z<=center.y+1;z++)for(int x=center.x-1;x<=center.x+1;x++)if(x>=0&&z>=0&&x<4&&z<4){wanted.Add($"AnchorIsland_Tile_{x}_{z}");order.Add(new Vector2Int(x,z));}
                order.Sort((a,b)=>(a-center).sqrMagnitude.CompareTo((b-center).sqrMagnitude));busy=true;
                var remove=new List<string>();for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.name.StartsWith("AnchorIsland_Tile_")&&!wanted.Contains(s.name))remove.Add(s.name);}
                foreach(var name in remove)yield return SceneManager.UnloadSceneAsync(name);
                foreach(var t in order){if(Tile(target.position)!=center)break;var name=$"AnchorIsland_Tile_{t.x}_{t.y}";if(SceneManager.GetSceneByName(name).isLoaded)continue;if(!Application.CanStreamedLevelBeLoaded(name)){Debug.LogError("Missing scene: "+name);yield break;}yield return SceneManager.LoadSceneAsync(name,LoadSceneMode.Additive);}
                var terrains=new Dictionary<Vector2Int,Terrain>();foreach(var t in Terrain.activeTerrains)terrains[Tile(t.transform.position+Vector3.one)]=t;
                foreach(var p in terrains){terrains.TryGetValue(p.Key+Vector2Int.left,out var l);terrains.TryGetValue(p.Key+Vector2Int.right,out var r);terrains.TryGetValue(p.Key+Vector2Int.up,out var u);terrains.TryGetValue(p.Key+Vector2Int.down,out var d);p.Value.SetNeighbors(l,u,r,d);}
                foreach(var t in Terrain.activeTerrains){var p=t.transform.position;var size=t.terrainData.size;if(p.x<=0&&p.x+size.x>=-380&&p.z<=0&&p.z+size.z>=-230&&!t.GetComponent<AnchorTrailTerrain>())t.gameObject.AddComponent<AnchorTrailTerrain>();}
                busy=false;yield return new WaitForSecondsRealtime(.2f);
            }
        }
    }
}
